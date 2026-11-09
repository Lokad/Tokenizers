using System.Security.Cryptography;
using System.Text;
using Lokad.Tokenizers.Tokenizer;

namespace Lokad.Tokenizers.Vocab;

/// <summary>Scored unigram segmentation for the supported XLM-R SentencePiece profile.</summary>
/// <remarks>Do not mutate Root or its descendants while encoding concurrently.</remarks>
public class SentencePieceModel
{
    private TrieNode root = new("");
    private PrefixMatcher<TrieNode>? matcher;
    private readonly object matcherLock = new();
    private volatile bool externallyMutable;
    /// <summary>The mutable public trie. Obtaining it disables the shared compiled snapshot.</summary>
    public TrieNode Root
    {
        get { externallyMutable = true; return root; }
        set { root = value ?? throw new ArgumentNullException(nameof(value)); externallyMutable = true; }
    }
    private float unknownScore = -100;
    // Keep the existing float comparisons (and legacy tie-breaking) with ample
    // headroom for any representable input length, including the unknown penalty.
    private const double MaximumScoreMagnitude = (double)float.MaxValue / int.MaxValue / 4;
    private static bool IsSupportedScore(float score) => float.IsFinite(score) && Math.Abs((double)score) <= MaximumScoreMagnitude;

    public static SentencePieceModel FromFile(string path) => FromModel(VocabHelper.OpenProtobufFile(path));

    internal static SentencePieceModel FromModel(ModelProto proto)
    {
        Validate(proto);
        return FromValidatedModel(proto);
    }

    internal static SentencePieceModel FromValidatedModel(ModelProto proto)
    {
        var model = new SentencePieceModel
        {
            unknownScore = proto.Pieces.Skip(3).Min(p => p.Score) - 10
        };
        for (int index = 3; index < proto.Pieces.Count; index++)
            model.Insert(proto.Pieces[index].Piece, proto.Pieces[index].Score, index);
        model.matcher = model.CompileMatcher();
        return model;
    }

    internal static void Validate(ModelProto proto)
    {
        var trainer = proto.TrainerSpec;
        var normalizer = proto.NormalizerSpec;
        if (trainer == null || trainer.ModelType != TrainerSpec.Types.ModelType.Unigram || trainer.ByteFallback ||
            trainer.UnkId != 0 || trainer.BosId != 1 || trainer.EosId != 2 || trainer.PadId != -1 ||
            trainer.TreatWhitespaceAsSuffix)
            throw new NotSupportedException("Only unigram XLM-R models with standard SentencePiece IDs and no byte fallback are supported");
        if (normalizer == null || normalizer.Name != "nmt_nfkc" || !normalizer.AddDummyPrefix ||
            !normalizer.RemoveExtraWhitespaces || !normalizer.EscapeWhitespaces || proto.DenormalizerSpec != null ||
            Convert.ToHexStringLower(SHA256.HashData(normalizer.PrecompiledCharsmap.Span)) !=
                "0942789e0111452f1cc446f70036e26d24458c3c7858c53cc12ca4d87531279b")
            throw new NotSupportedException("Unsupported XLM-R normalization profile");
        if (proto.Pieces.Count < 4 || trainer.VocabSize != proto.Pieces.Count ||
            proto.Pieces[0].Piece != "<unk>" || proto.Pieces[1].Piece != "<s>" || proto.Pieces[2].Piece != "</s>" ||
            proto.Pieces[0].Type != ModelProto.Types.SentencePiece.Types.Type.Unknown ||
            proto.Pieces[1].Type != ModelProto.Types.SentencePiece.Types.Type.Control ||
            proto.Pieces[2].Type != ModelProto.Types.SentencePiece.Types.Type.Control)
            throw new InvalidDataException("Invalid XLM-R vocabulary or special-token layout");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < proto.Pieces.Count; i++)
        {
            var piece = proto.Pieces[i];
            if (piece.Piece.Length == 0 || !seen.Add(piece.Piece) || !float.IsFinite(piece.Score))
                throw new InvalidDataException("Vocabulary pieces must be nonempty, unique and have finite scores");
            if (!IsSupportedScore(piece.Score))
                throw new InvalidDataException($"model.pieces[{i}].score exceeds the supported cumulative score range");
            if (i >= 3 && piece.Type != ModelProto.Types.SentencePiece.Types.Type.Normal)
                throw new NotSupportedException("Only normal pieces after the three SentencePiece special entries are supported");
        }
    }

    private void Insert(string text, float score, int index)
    {
        var node = root;
        int utf16 = 0, scalars = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            utf16 += rune.Utf16SequenceLength;
            node = node.GetOrAddChild(rune, text, utf16, ++scalars);
        }
        node.Text = text;
        node.End = true;
        node.Score = score;
        node.Index = index;
    }

    public List<TrieNode> CommonPrefixSearch(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        UnicodeProfile.ValidateUtf16(text);
        var result = new List<TrieNode>();
        var node = root;
        foreach (Rune rune in text.EnumerateRunes())
        {
            var child = node.FindChild(rune);
            if (child == null) break;
            node = child;
            if (node.End) result.Add(node);
        }
        if (result.Count != 0) externallyMutable = true;
        return result;
    }

    /// <summary>Compute best predecessor nodes for valid UTF-16, without constructing suffix strings.</summary>
    /// <remarks>Raw segmentation performs no cleaning or normalization. Malformed UTF-16 is rejected.</remarks>
    public List<Node?> DecodeForwardTokenRef(Token token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var path = FindPredecessors(token.Text);
        var references = TokenAlignment.References(token);
        var result = new List<Node?>(path.Runes.Length + 1) { null };
        for (int end = 1; end <= path.Runes.Length; end++) result.Add(Materialize(path, end, references));
        return result;
    }

    private readonly record struct Path(Rune[] Runes, float[] Scores, int[] Starts, TrieNode?[] Pieces);

    private PrefixMatcher<TrieNode> CompileMatcher() => PrefixMatcher<TrieNode>.Create(root, n => n.Edges, n => (n.End, n));

    private PrefixMatcher<TrieNode> GetMatcher()
    {
        // Dictionary mutations through the public trie cannot be intercepted. Callers
        // obtaining nodes retain live mutation semantics through a fresh snapshot.
        if (externallyMutable) return CompileMatcher();
        lock (matcherLock) return matcher ??= CompileMatcher();
    }

    private Path FindPredecessors(string text)
    {
        UnicodeProfile.ValidateUtf16(text);
        Rune[] runes = text.EnumerateRunes().ToArray();
        var scores = Enumerable.Repeat(float.NegativeInfinity, runes.Length + 1).ToArray();
        var starts = Enumerable.Repeat(-1, runes.Length + 1).ToArray();
        var pieces = new TrieNode?[runes.Length + 1];
        scores[0] = 0;
        if (runes.Length == 0) return new(runes, scores, starts, pieces);
        var matching = GetMatcher();
        int state = 0;
        for (int end = 1; end <= runes.Length; end++)
        {
            state = matching.Step(state, runes[end - 1].Value);
            bool single = false;
            // Earlier starts (longest pieces) win float ties, as in the original
            // start-major dynamic program. Unknown length-one edges are last.
            for (int match = matching.FirstMatch(state); match != 0; match = matching.NextMatch(match))
            {
                var node = matching.Value(match);
                int length = matching.Length(match), start = end - length;
                if (!IsSupportedScore(node.Score)) throw new InvalidDataException("Model piece score exceeds the supported cumulative score range");
                if (length == 1) single = true;
                float score = scores[start] + node.Score;
                if (!float.IsFinite(score)) throw new InvalidDataException("Model cumulative score is not finite");
                if (score > scores[end])
                {
                    scores[end] = score;
                    starts[end] = start;
                    pieces[end] = node;
                }
            }
            if (!single && scores[end - 1] + unknownScore > scores[end])
            {
                scores[end] = scores[end - 1] + unknownScore;
                starts[end] = end - 1;
                pieces[end] = null;
            }
            if (!float.IsFinite(scores[end])) throw new InvalidDataException("Model has an unreachable or non-finite segmentation path");
        }
        return new(runes, scores, starts, pieces);
    }

    private static Node Materialize(Path path, int end, uint[][]? references)
    {
        int start = path.Starts[end];
        if (start < 0 || start >= end || !float.IsFinite(path.Scores[end]))
            throw new InvalidDataException("Model has an invalid or unreachable predecessor");
        var piece = path.Pieces[end];
        var mapped = references?.AsSpan(start, end - start).ToArray();
        return Node.Create(piece?.Text ?? path.Runes[start].ToString(), path.Scores[end], piece?.Index ?? 0, start, end, mapped ?? []);
    }

    internal List<Token> Tokenize(Token token)
    {
        var path = FindPredecessors(token.Text);
        var references = token.TracksOffsets ? TokenAlignment.References(token) : null;
        var nodes = new List<Node>();
        for (int end = path.Runes.Length; end > 0; end = path.Starts[end]) nodes.Add(Materialize(path, end, references));
        nodes.Reverse();
        return ParseNodesToTokens(nodes, token.TracksOffsets);
    }

    public List<Node> DecodeBackward(Node?[] nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        if (nodes.Length == 0) return new();
        var result = new List<Node>();
        int cursor = nodes.Length - 1;
        while (cursor > 0)
        {
            Node next = nodes[cursor] ?? throw new ArgumentException("Missing predecessor", nameof(nodes));
            if (next.End != cursor || next.Start < 0 || next.Start >= cursor)
                throw new ArgumentException("Invalid predecessor chain", nameof(nodes));
            result.Add(next);
            cursor = next.Start;
        }
        if (nodes[0] != null) throw new ArgumentException("The origin must be a null sentinel", nameof(nodes));
        result.Reverse();
        return result;
    }

    public List<Token> ParseNodesToTokens(List<Node> nodes) => ParseNodesToTokens(nodes, true);

    private List<Token> ParseNodesToTokens(List<Node> nodes, bool trackOffsets)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var result = new List<Token>();
        for (int i = 0; i < nodes.Count;)
        {
            if (nodes[i].Index != 0)
            {
                var node = nodes[i++];
                var piece = new Token(node.Text, trackOffsets);
                if (trackOffsets) TokenAlignment.Set(piece, node.Text,
                    node.References);
                result.Add(piece);
                continue;
            }
            var text = new StringBuilder();
            var references = trackOffsets ? new List<uint[]>() : null;
            do
            {
                Node node = nodes[i++];
                text.Append(node.Text);
                references?.AddRange(node.References);
            } while (i < nodes.Count && nodes[i].Index == 0);
            var token = new Token(text.ToString(), trackOffsets) { Mask = Mask.Unknown };
            if (references != null) TokenAlignment.Set(token, token.Text, references.ToArray());
            result.Add(token);
        }
        PopulateMasks(result, TokenizationUtils.Metaspace);
        return result;
    }

    public void PopulateMasks(List<Token> tokens, char whitespaceToken)
    {
        var previous = Mask.None;
        foreach (Token token in tokens)
        {
            if (token.Mask is Mask.Unknown or Mask.Special) { previous = token.Mask; continue; }
            if (Rune.TryGetRuneAt(token.Text, 0, out var rune) && rune.Utf16SequenceLength == token.Text.Length)
            {
                if (UnicodeProfile.IsPunctuation(rune)) { token.Mask = previous = Mask.Punctuation; continue; }
                if (UnicodeProfile.IsWhiteSpace(rune)) { token.Mask = previous = Mask.Whitespace; continue; }
            }
            token.Mask = !token.Text.StartsWith(whitespaceToken) && previous is not Mask.Punctuation and not Mask.Whitespace
                ? Mask.Continuation : Mask.None;
            previous = token.Mask;
        }
    }
}

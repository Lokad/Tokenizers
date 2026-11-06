using System.Security.Cryptography;
using System.Text;
using Lokad.Tokenizers.Tokenizer;

namespace Lokad.Tokenizers.Vocab;

/// <summary>Scored unigram segmentation for the supported XLM-R SentencePiece profile.</summary>
/// <remarks>Do not mutate Root or its descendants while encoding concurrently.</remarks>
public class SentencePieceModel
{
    public TrieNode Root { get; set; } = new("");
    private float unknownScore = -100;

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
            if (i >= 3 && piece.Type != ModelProto.Types.SentencePiece.Types.Type.Normal)
                throw new NotSupportedException("Only normal pieces after the three SentencePiece special entries are supported");
        }
    }

    private void Insert(string text, float score, int index)
    {
        var node = Root;
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!node.Children.TryGetValue(rune, out var child))
                node.Children.Add(rune, child = new TrieNode(node.Text + rune));
            node = child;
        }
        node.End = true;
        node.Score = score;
        node.Index = index;
    }

    public List<TrieNode> CommonPrefixSearch(string text)
    {
        var result = new List<TrieNode>();
        var node = Root;
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!node.Children.TryGetValue(rune, out node)) break;
            if (node.End) result.Add(node);
        }
        return result;
    }

    /// <summary>Compute best predecessor nodes once, without constructing suffix strings.</summary>
    public List<Node?> DecodeForwardTokenRef(Token token)
    {
        Rune[] runes = token.Text.EnumerateRunes().ToArray();
        uint[][] references = TokenAlignment.References(token);
        var scores = Enumerable.Repeat(float.NegativeInfinity, runes.Length + 1).ToArray();
        var starts = new int[runes.Length + 1];
        var pieces = new TrieNode?[runes.Length + 1];
        scores[0] = 0;
        for (int start = 0; start < runes.Length; start++)
        {
            bool single = false;
            var node = Root;
            for (int end = start; end < runes.Length; end++)
            {
                if (!node.Children.TryGetValue(runes[end], out node)) break;
                if (!node.End) continue;
                if (end == start) single = true;
                float score = scores[start] + node.Score;
                if (score > scores[end + 1])
                {
                    scores[end + 1] = score;
                    starts[end + 1] = start;
                    pieces[end + 1] = node;
                }
            }
            if (!single && scores[start] + unknownScore > scores[start + 1])
            {
                scores[start + 1] = scores[start] + unknownScore;
                starts[start + 1] = start;
                pieces[start + 1] = null;
            }
        }
        var result = new List<Node?>(runes.Length + 1) { null };
        for (int end = 1; end <= runes.Length; end++)
        {
            int start = starts[end];
            var piece = pieces[end];
            uint[][] mapped = references.AsSpan(start, end - start).ToArray();
            uint[] positions = mapped.SelectMany(r => r).Distinct().Order().ToArray();
            result.Add(new Node(piece?.Text ?? runes[start].ToString(), scores[end], piece?.Index ?? 0,
                start, end, positions) { ScalarReferences = mapped });
        }
        return result;
    }

    public List<Node> DecodeBackward(Node?[] nodes)
    {
        if (nodes.Length == 0) return new();
        var result = new List<Node>();
        for (Node? next = nodes[^1]; next != null; next = nodes[next.Start])
        {
            if (next.Start < 0 || next.Start >= next.End || next.End >= nodes.Length)
                throw new ArgumentException("Invalid predecessor chain", nameof(nodes));
            result.Add(next);
        }
        result.Reverse();
        return result;
    }

    public List<Token> ParseNodesToTokens(List<Node> nodes)
    {
        var result = new List<Token>();
        for (int i = 0; i < nodes.Count;)
        {
            var text = new StringBuilder();
            var references = new List<uint[]>();
            bool unknown = nodes[i].Index == 0;
            do
            {
                Node node = nodes[i++];
                text.Append(node.Text);
                references.AddRange(node.ScalarReferences ?? node.ReferenceOffsets.Select(p => new[] { p }).ToArray());
            } while (unknown && i < nodes.Count && nodes[i].Index == 0);
            var token = new Token(text.ToString()) { Mask = unknown ? Mask.Unknown : Mask.None };
            TokenAlignment.Set(token, token.Text, references.ToArray());
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
                if (Rune.IsPunctuation(rune)) { token.Mask = previous = Mask.Punctuation; continue; }
                if (Rune.IsWhiteSpace(rune)) { token.Mask = previous = Mask.Whitespace; continue; }
            }
            token.Mask = !token.Text.StartsWith(whitespaceToken) && previous is not Mask.Punctuation and not Mask.Whitespace
                ? Mask.Continuation : Mask.None;
            previous = token.Mask;
        }
    }
}

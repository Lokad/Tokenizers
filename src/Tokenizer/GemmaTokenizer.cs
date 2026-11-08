using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Immutable, managed encoder for Gemma's space-replacing, byte-fallback BPE
/// tokenizer.json pipeline. Instances can be shared across concurrent callers.
/// </summary>
public sealed class GemmaTokenizer
{
    readonly Dictionary<Rune, int> characters = new();
    readonly Dictionary<(int Left, int Right), Merge> merges = new();
    readonly int[] bytes = new int[256];
    readonly LiteralTokenMatcher addedTokens;
    readonly int maximumTokenBytes;

    /// <summary>SHA-256 of the exact tokenizer JSON loaded by this instance.</summary>
    public string TokenizerSha256 { get; }

    /// <summary>Number of entries in the BPE vocabulary.</summary>
    public int VocabularySize { get; }

    /// <summary>Load a local tokenizer.json. Unsupported pipeline settings fail explicitly.</summary>
    public GemmaTokenizer(string tokenizerPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenizerPath);
        byte[] data = File.ReadAllBytes(tokenizerPath);
        TokenizerSha256 = Convert.ToHexStringLower(SHA256.HashData(data));
        using var document = JsonAsset.Parse(data);
        var root = JsonAsset.Object(document.RootElement, "tokenizer");
        var model = JsonAsset.Object(JsonAsset.Required(root, "model", "tokenizer"), "model");
        Require(JsonAsset.String(root, "version", "tokenizer") == "1.0", "JSON version");
        Require(IsNull(root, "truncation", "tokenizer") && IsNull(root, "padding", "tokenizer"), "preconfigured truncation/padding");
        Require(JsonAsset.String(model, "type", "model") == "BPE" && IsNull(model, "dropout", "model") &&
            IsNull(model, "continuing_subword_prefix", "model") && IsNull(model, "end_of_word_suffix", "model") &&
            JsonAsset.String(model, "unk_token", "model") == "<unk>" &&
            JsonAsset.Bool(model, "fuse_unk", "model") && JsonAsset.Bool(model, "byte_fallback", "model") &&
            !JsonAsset.Bool(model, "ignore_merges", "model"), "BPE settings");
        var normalizer = JsonAsset.Object(JsonAsset.Required(root, "normalizer", "tokenizer"), "normalizer");
        var split = JsonAsset.Object(JsonAsset.Required(root, "pre_tokenizer", "tokenizer"), "pre_tokenizer");
        Require(JsonAsset.String(normalizer, "type", "normalizer") == "Replace" &&
            Pattern(normalizer, "normalizer") == " " && JsonAsset.String(normalizer, "content", "normalizer") == "\u2581", "space normalizer");
        Require(JsonAsset.String(split, "type", "pre_tokenizer") == "Split" &&
            Pattern(split, "pre_tokenizer") == " " && JsonAsset.String(split, "behavior", "pre_tokenizer") == "MergedWithPrevious" &&
            !JsonAsset.Bool(split, "invert", "pre_tokenizer"), "pre-tokenizer");

        var ids = new HashSet<int>();
        var vocabulary = new Dictionary<string, int>(StringComparer.Ordinal);
        // Vocabulary duplicates are checked by TryAdd while constructing the lookup.
        foreach (var entry in JsonAsset.Object(JsonAsset.Required(model, "vocab", "model"), "model.vocab", checkDuplicates: false).EnumerateObject())
        {
            string text = JsonAsset.Name(entry, "model.vocab");
            int id = JsonAsset.Int(entry.Value, "model.vocab");
            if (text.Length == 0 || id < 0 || !ids.Add(id) || !vocabulary.TryAdd(text, id))
                throw new InvalidDataException("Invalid or duplicate model.vocab entry: " + text);
            maximumTokenBytes = Math.Max(maximumTokenBytes, Encoding.UTF8.GetByteCount(text));
            if (Rune.DecodeFromUtf16(text, out Rune rune, out int consumed) == OperationStatus.Done && consumed == text.Length)
                characters.Add(rune, id);
        }
        VocabularySize = vocabulary.Count;
        Require(vocabulary.GetValueOrDefault("<pad>", -1) == 0 && vocabulary.GetValueOrDefault("<eos>", -1) == 1 &&
            vocabulary.GetValueOrDefault("<bos>", -1) == 2 && vocabulary.GetValueOrDefault("<unk>", -1) == 3, "PAD/EOS/BOS/UNK IDs");
        for (int value = 0; value < bytes.Length; value++)
        {
            if (!vocabulary.TryGetValue("<0x" + value.ToString("X2", CultureInfo.InvariantCulture) + ">", out bytes[value]))
                throw new InvalidDataException("Missing model.vocab UTF-8 byte fallback token: <0x" + value.ToString("X2", CultureInfo.InvariantCulture) + ">");
        }
        int rank = 0;
        foreach (var pair in JsonAsset.Array(JsonAsset.Required(model, "merges", "model"), "model.merges").EnumerateArray())
        {
            JsonAsset.Array(pair, "model.merges[]");
            Require(pair.GetArrayLength() == 2, "BPE merge format");
            string left = JsonAsset.String(pair[0], "model.merges[][0]"), right = JsonAsset.String(pair[1], "model.merges[][1]");
            if (!vocabulary.TryGetValue(left, out int a) || !vocabulary.TryGetValue(right, out int b) ||
                !vocabulary.TryGetValue(left + right, out int merged) || !merges.TryAdd((a, b), new Merge(rank++, merged)))
                throw new InvalidDataException("Invalid or duplicate model.merges entry: " + left + " + " + right);
        }
        var added = new List<(string Text, int Id)>();
        foreach (var entry in JsonAsset.Array(JsonAsset.Required(root, "added_tokens", "tokenizer"), "added_tokens").EnumerateArray())
        {
            var token = JsonAsset.Object(entry, "added_tokens[]");
            Require(!JsonAsset.Bool(token, "single_word", "added_tokens[]") && !JsonAsset.Bool(token, "lstrip", "added_tokens[]") &&
                !JsonAsset.Bool(token, "rstrip", "added_tokens[]") && !JsonAsset.Bool(token, "normalized", "added_tokens[]") &&
                JsonAsset.Bool(token, "special", "added_tokens[]"), "added-token flags");
            string text = JsonAsset.String(token, "content", "added_tokens[]");
            int id = JsonAsset.Int(JsonAsset.Required(token, "id", "added_tokens[]"), "added_tokens[].id");
            if (!vocabulary.TryGetValue(text, out int actual) || actual != id || added.Any(t => t.Text == text))
                throw new InvalidDataException("Invalid added_tokens[].content/id: " + text);
            added.Add((text, id));
        }
        addedTokens = new LiteralTokenMatcher(added.Select(token => (token.Text, (long)token.Id)));
        var post = JsonAsset.Object(JsonAsset.Required(root, "post_processor", "tokenizer"), "post_processor");
        var single = JsonAsset.Array(JsonAsset.Required(post, "single", "post_processor"), "post_processor.single");
        Require(JsonAsset.String(post, "type", "post_processor") == "TemplateProcessing" && single.GetArrayLength() == 3 &&
            TemplateToken(single[0], "SpecialToken", "<bos>") && TemplateToken(single[1], "Sequence", "A") &&
            TemplateToken(single[2], "SpecialToken", "<eos>"), "single-sequence template");
        var specials = JsonAsset.Object(JsonAsset.Required(post, "special_tokens", "post_processor"), "post_processor.special_tokens");
        foreach (var (text, id) in new[] { ("<bos>", 2), ("<eos>", 1) })
        {
            var token = JsonAsset.Object(JsonAsset.Required(specials, text, "post_processor.special_tokens"), "post_processor.special_tokens[]");
            var tokenIds = JsonAsset.Array(JsonAsset.Required(token, "ids", "post_processor.special_tokens[]"), "post_processor.special_tokens[].ids");
            var texts = JsonAsset.Array(JsonAsset.Required(token, "tokens", "post_processor.special_tokens[]"), "post_processor.special_tokens[].tokens");
            Require(JsonAsset.String(token, "id", "post_processor.special_tokens[]") == text && tokenIds.GetArrayLength() == 1 &&
                JsonAsset.Int(tokenIds[0], "post_processor.special_tokens[].ids[]") == id && texts.GetArrayLength() == 1 &&
                JsonAsset.String(texts[0], "post_processor.special_tokens[].tokens[]") == text && added.Contains((text, id)), "template token IDs");
        }
    }

    static bool TemplateToken(JsonElement element, string kind, string id)
    {
        JsonAsset.Object(element, "post_processor.single[]");
        if (!element.TryGetProperty(kind, out var value)) return false;
        var token = JsonAsset.Object(value, "post_processor.single[]");
        return JsonAsset.String(token, "id", "post_processor.single[]") == id &&
            JsonAsset.Int(JsonAsset.Required(token, "type_id", "post_processor.single[]"), "post_processor.single[].type_id") == 0;
    }

    static string Pattern(JsonElement element, string path) => JsonAsset.String(
        JsonAsset.Object(JsonAsset.Required(element, "pattern", path), path + ".pattern"), "String", path + ".pattern");

    static bool IsNull(JsonElement element, string property, string path) => JsonAsset.Required(element, property, path).ValueKind == JsonValueKind.Null;

    static void Require(bool condition, string setting)
    {
        if (!condition) throw new NotSupportedException("Unsupported Gemma tokenizer " + setting);
    }

    /// <summary>Encode with BOS and EOS, preserving whitespace and Unicode distinctions.</summary>
    public long[] Encode(string text) => Encode(text, true);

    /// <summary>Encode text, optionally adding BOS/EOS. Literal added tokens are always recognized.</summary>
    public long[] Encode(string text, bool addSpecialTokens) => EncodeCore(text, addSpecialTokens, int.MaxValue);

    private long[] EncodeCore(string text, bool addSpecialTokens, int maximumTokens)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<long>();
        if (addSpecialTokens) result.Add(2);
        int contentLimit = maximumTokens - (addSpecialTokens ? 1 : 0);
        int start = 0, position = 0;
        long ordinaryBytes = 0;
        var matches = addedTokens.FindMatches(text);
        while (position < text.Length)
        {
            if (addedTokens.TryMatch(matches, position, out var match))
            {
                EncodeOrdinary(text.AsSpan(start, position - start), result, contentLimit, ordinaryBytes);
                if (result.Count >= contentLimit) ThrowTokenLimit();
                result.Add(match.Id);
                position += match.Text.Length;
                start = position;
                ordinaryBytes = 0;
            }
            else
            {
                if (Rune.DecodeFromUtf16(text.AsSpan(position), out Rune rune, out int consumed) != OperationStatus.Done)
                    throw new ArgumentException("Text contains invalid UTF-16", nameof(text));
                ordinaryBytes += rune.Value == ' ' ? 3 : rune.Utf8SequenceLength;
                position += consumed;
            }
        }
        EncodeOrdinary(text.AsSpan(start), result, contentLimit, ordinaryBytes);
        if (addSpecialTokens) result.Add(1);
        return result.ToArray();
    }

    private static void ThrowTokenLimit() => throw new ArgumentException("Input exceeds the token limit", "maximumTokens");

    /// <summary>Encode a nonempty batch, rejecting overflow and right-padding to the longest row.</summary>
    public GemmaTokenizedBatch EncodeBatch(IReadOnlyList<string> texts, int maximumTokens)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count == 0) throw new ArgumentException("Batch must not be empty", nameof(texts));
        if (maximumTokens < 2) throw new ArgumentOutOfRangeException(nameof(maximumTokens));
        var rows = new long[texts.Count][];
        var counts = new int[rows.Length];
        int width = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            try { rows[i] = EncodeCore(texts[i], true, maximumTokens); }
            catch (ArgumentException error) when (error.ParamName == "maximumTokens")
            {
                throw new ArgumentException($"Input {i} exceeds the token limit {maximumTokens}", nameof(texts), error);
            }
            counts[i] = rows[i].Length;
            width = Math.Max(width, counts[i]);
        }
        var inputIds = new long[checked(rows.Length * width)];
        var mask = new long[inputIds.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].CopyTo(inputIds, i * width);
            mask.AsSpan(i * width, rows[i].Length).Fill(1);
        }
        return new GemmaTokenizedBatch(inputIds, mask, counts, rows.Length, width);
    }

    void EncodeOrdinary(ReadOnlySpan<char> text, List<long> result, int contentLimit, long normalizedBytes)
    {
        if (text.IsEmpty) return;
        // Every final piece represents at most maximumTokenBytes normalized bytes.
        // Unlike a character-count estimate, this lower bound remains valid after arbitrary BPE merges.
        if (normalizedBytes > (long)maximumTokenBytes * (contentLimit - result.Count)) ThrowTokenLimit();
        var initial = new List<int>();
        Span<byte> encoded = stackalloc byte[4];
        while (!text.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(text, out Rune rune, out int consumed) != OperationStatus.Done)
                throw new ArgumentException("Text contains invalid UTF-16", nameof(text));
            if (rune.Value == ' ') rune = new Rune(0x2581);
            if (characters.TryGetValue(rune, out int id)) initial.Add(id);
            else
            {
                int length = rune.EncodeToUtf8(encoded);
                for (int i = 0; i < length; i++) initial.Add(bytes[encoded[i]]);
            }
            text = text[consumed..];
        }
        if (initial.Count == 0) return;
        var symbols = new Symbol[initial.Count];
        for (int i = 0; i < symbols.Length; i++)
            symbols[i] = new Symbol { Id = initial[i], Previous = i - 1, Next = i + 1 < symbols.Length ? i + 1 : -1, Alive = true };
        var queue = new PriorityQueue<Candidate, (int Rank, int Position)>();
        void Enqueue(int left)
        {
            if (left < 0) return;
            ref Symbol a = ref symbols[left];
            int right = a.Next;
            if (right >= 0 && merges.TryGetValue((a.Id, symbols[right].Id), out Merge merge))
                queue.Enqueue(new Candidate(left, right, a.Version, symbols[right].Version, merge.Id), (merge.Rank, left));
        }
        for (int i = 0; i < symbols.Length - 1; i++) Enqueue(i);
        while (queue.TryDequeue(out var candidate, out _))
        {
            ref Symbol a = ref symbols[candidate.Left];
            ref Symbol b = ref symbols[candidate.Right];
            if (!a.Alive || !b.Alive || a.Next != candidate.Right || a.Version != candidate.LeftVersion || b.Version != candidate.RightVersion)
                continue;
            a.Id = candidate.Id;
            a.Next = b.Next;
            a.Version++;
            b.Alive = false;
            b.Version++;
            if (a.Next >= 0) symbols[a.Next].Previous = candidate.Left;
            Enqueue(a.Previous);
            Enqueue(candidate.Left);
        }
        for (int i = 0; i >= 0; i = symbols[i].Next)
        {
            if (result.Count >= contentLimit) ThrowTokenLimit();
            result.Add(symbols[i].Id);
        }
    }

    readonly record struct Merge(int Rank, int Id);
    readonly record struct Candidate(int Left, int Right, int LeftVersion, int RightVersion, int Id);
    struct Symbol
    {
        public int Id, Previous, Next, Version;
        public bool Alive;
    }
}

/// <summary>Owned, row-major input arrays. AttentionMask is 1 for tokens and 0 for right padding.</summary>
public sealed record GemmaTokenizedBatch(long[] InputIds, long[] AttentionMask, int[] TokenCounts, int BatchSize, int SequenceLength);

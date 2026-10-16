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
    readonly Dictionary<string, int> vocabulary = new(StringComparer.Ordinal);
    readonly Dictionary<(int Left, int Right), Merge> merges = new();
    readonly List<(string Text, int Id)> added = new();
    readonly int[] bytes = new int[256];

    /// <summary>SHA-256 of the exact tokenizer JSON loaded by this instance.</summary>
    public string TokenizerSha256 { get; }

    /// <summary>Number of entries in the BPE vocabulary.</summary>
    public int VocabularySize => vocabulary.Count;

    /// <summary>Load a local tokenizer.json. Unsupported pipeline settings fail explicitly.</summary>
    public GemmaTokenizer(string tokenizerPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenizerPath);
        byte[] data = File.ReadAllBytes(tokenizerPath);
        TokenizerSha256 = Convert.ToHexStringLower(SHA256.HashData(data));
        using var document = JsonDocument.Parse(data);
        JsonElement root = document.RootElement, model = root.GetProperty("model");
        Require(root.GetProperty("version").GetString() == "1.0", "JSON version");
        Require(IsNull(root, "truncation") && IsNull(root, "padding"), "preconfigured truncation/padding");
        Require(model.GetProperty("type").GetString() == "BPE" && IsNull(model, "dropout") &&
            IsNull(model, "continuing_subword_prefix") && IsNull(model, "end_of_word_suffix") &&
            model.GetProperty("unk_token").GetString() == "<unk>" &&
            model.GetProperty("fuse_unk").GetBoolean() && model.GetProperty("byte_fallback").GetBoolean() &&
            !model.GetProperty("ignore_merges").GetBoolean(), "BPE settings");
        JsonElement normalizer = root.GetProperty("normalizer"), split = root.GetProperty("pre_tokenizer");
        Require(normalizer.GetProperty("type").GetString() == "Replace" &&
            normalizer.GetProperty("pattern").GetProperty("String").GetString() == " " &&
            normalizer.GetProperty("content").GetString() == "\u2581", "space normalizer");
        Require(split.GetProperty("type").GetString() == "Split" &&
            split.GetProperty("pattern").GetProperty("String").GetString() == " " &&
            split.GetProperty("behavior").GetString() == "MergedWithPrevious" &&
            !split.GetProperty("invert").GetBoolean(), "pre-tokenizer");

        var ids = new HashSet<int>();
        foreach (var entry in model.GetProperty("vocab").EnumerateObject())
        {
            int id = entry.Value.GetInt32();
            if (entry.Name.Length == 0 || id < 0 || !ids.Add(id) || !vocabulary.TryAdd(entry.Name, id))
                throw new InvalidDataException("Invalid or duplicate vocabulary entry");
        }
        Require(vocabulary.GetValueOrDefault("<pad>", -1) == 0 && vocabulary.GetValueOrDefault("<eos>", -1) == 1 &&
            vocabulary.GetValueOrDefault("<bos>", -1) == 2 && vocabulary.GetValueOrDefault("<unk>", -1) == 3,
            "PAD/EOS/BOS/UNK IDs");
        for (int value = 0; value < bytes.Length; value++)
        {
            if (!vocabulary.TryGetValue("<0x" + value.ToString("X2", CultureInfo.InvariantCulture) + ">", out bytes[value]))
                throw new InvalidDataException("Missing UTF-8 byte fallback token");
        }
        int rank = 0;
        foreach (var pair in model.GetProperty("merges").EnumerateArray())
        {
            Require(pair.ValueKind == JsonValueKind.Array && pair.GetArrayLength() == 2, "BPE merge format");
            string left = pair[0].GetString()!, right = pair[1].GetString()!;
            if (!vocabulary.TryGetValue(left, out int a) || !vocabulary.TryGetValue(right, out int b) ||
                !vocabulary.TryGetValue(left + right, out int merged) || !merges.TryAdd((a, b), new Merge(rank++, merged)))
                throw new InvalidDataException("Invalid or duplicate BPE merge");
        }
        foreach (var token in root.GetProperty("added_tokens").EnumerateArray())
        {
            Require(!token.GetProperty("single_word").GetBoolean() && !token.GetProperty("lstrip").GetBoolean() &&
                !token.GetProperty("rstrip").GetBoolean() && !token.GetProperty("normalized").GetBoolean() &&
                token.GetProperty("special").GetBoolean(), "added-token flags");
            string text = token.GetProperty("content").GetString()!;
            int id = token.GetProperty("id").GetInt32();
            if (text.Length == 0 || !vocabulary.TryGetValue(text, out int actual) || actual != id || added.Any(t => t.Text == text))
                throw new InvalidDataException("Added token differs from vocabulary");
            added.Add((text, id));
        }
        JsonElement post = root.GetProperty("post_processor"), single = post.GetProperty("single");
        Require(post.GetProperty("type").GetString() == "TemplateProcessing" && single.GetArrayLength() == 3 &&
            TemplateToken(single[0], "SpecialToken", "<bos>") && TemplateToken(single[1], "Sequence", "A") &&
            TemplateToken(single[2], "SpecialToken", "<eos>"), "single-sequence template");
        JsonElement specials = post.GetProperty("special_tokens");
        foreach (var (text, id) in new[] { ("<bos>", 2), ("<eos>", 1) })
        {
            JsonElement token = specials.GetProperty(text);
            Require(token.GetProperty("id").GetString() == text && token.GetProperty("ids").GetArrayLength() == 1 &&
                token.GetProperty("ids")[0].GetInt32() == id && token.GetProperty("tokens").GetArrayLength() == 1 &&
                token.GetProperty("tokens")[0].GetString() == text && added.Contains((text, id)), "template token IDs");
        }
    }

    static bool TemplateToken(JsonElement element, string kind, string id) =>
        element.TryGetProperty(kind, out var token) && token.GetProperty("id").GetString() == id &&
        token.GetProperty("type_id").GetInt32() == 0;

    static bool IsNull(JsonElement element, string property) => element.GetProperty(property).ValueKind == JsonValueKind.Null;

    static void Require(bool condition, string setting)
    {
        if (!condition) throw new NotSupportedException("Unsupported Gemma tokenizer " + setting);
    }

    /// <summary>Encode with BOS and EOS, preserving whitespace and Unicode distinctions.</summary>
    public long[] Encode(string text) => Encode(text, true);

    /// <summary>Encode text, optionally adding BOS/EOS. Literal added tokens are always recognized.</summary>
    public long[] Encode(string text, bool addSpecialTokens)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<long>();
        if (addSpecialTokens) result.Add(2);
        int offset = 0;
        while (offset < text.Length)
        {
            int next = text.Length;
            (string Text, int Id) selected = ("", 0);
            foreach (var token in added)
            {
                int position = text.IndexOf(token.Text, offset, StringComparison.Ordinal);
                if (position >= 0 && (position < next || position == next && token.Text.Length > selected.Text.Length))
                {
                    next = position;
                    selected = token;
                }
            }
            EncodeOrdinary(text.AsSpan(offset, next - offset), result);
            if (next == text.Length) break;
            result.Add(selected.Id);
            offset = next + selected.Text.Length;
        }
        if (addSpecialTokens) result.Add(1);
        return result.ToArray();
    }

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
            rows[i] = Encode(texts[i]);
            counts[i] = rows[i].Length;
            if (counts[i] > maximumTokens)
                throw new ArgumentException($"Input {i} requires {counts[i]} tokens; limit is {maximumTokens}", nameof(texts));
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

    void EncodeOrdinary(ReadOnlySpan<char> text, List<long> result)
    {
        var initial = new List<int>();
        Span<byte> encoded = stackalloc byte[4];
        while (!text.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(text, out Rune rune, out int consumed) != OperationStatus.Done)
                throw new ArgumentException("Text contains invalid UTF-16", nameof(text));
            if (rune.Value == ' ') rune = new Rune(0x2581);
            string symbol = rune.ToString();
            if (vocabulary.TryGetValue(symbol, out int id)) initial.Add(id);
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
        for (int i = 0; i >= 0; i = symbols[i].Next) result.Add(symbols[i].Id);
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

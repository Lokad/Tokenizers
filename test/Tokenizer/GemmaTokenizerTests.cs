using System.Text.Json;
using System.Text.Json.Nodes;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class GemmaTokenizerTests
{
    internal static string FixtureJson()
    {
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["<pad>"] = 0, ["<eos>"] = 1, ["<bos>"] = 2, ["<unk>"] = 3, ["<mask>"] = 4,
            ["a"] = 10, ["b"] = 11, ["c"] = 12, ["▁"] = 13, ["\t"] = 14, ["\n"] = 15, ["é"] = 16,
            ["bc"] = 30, ["ab"] = 21, ["abc"] = 22, ["aa"] = 23, ["aaa"] = 24,
        };
        for (int i = 0; i < 256; i++) vocab.Add($"<0x{i:X2}>", 100 + i);
        object Template(string kind, string id) => new Dictionary<string, object> { [kind] = new { id, type_id = 0 } };
        return JsonSerializer.Serialize(new
        {
            version = "1.0", truncation = (object?)null, padding = (object?)null,
            model = new { type = "BPE", dropout = (object?)null, unk_token = "<unk>", continuing_subword_prefix = (object?)null,
                end_of_word_suffix = (object?)null, fuse_unk = true, byte_fallback = true, ignore_merges = false, vocab,
                merges = new[] { new[] { "b", "c" }, new[] { "a", "b" }, new[] { "ab", "c" }, new[] { "a", "a" }, new[] { "aa", "a" } } },
            added_tokens = new[] { "<pad>", "<eos>", "<bos>", "<unk>", "<mask>" }.Select(content =>
                new { id = vocab[content], content, single_word = false, lstrip = false, rstrip = false, normalized = false, special = true }),
            normalizer = new { type = "Replace", pattern = new { String = " " }, content = "▁" },
            pre_tokenizer = new { type = "Split", pattern = new { String = " " }, behavior = "MergedWithPrevious", invert = false },
            post_processor = new { type = "TemplateProcessing", single = new[] { Template("SpecialToken", "<bos>"), Template("Sequence", "A"), Template("SpecialToken", "<eos>") },
                special_tokens = new Dictionary<string, object> { ["<bos>"] = new { id = "<bos>", ids = new[] { 2 }, tokens = new[] { "<bos>" } },
                    ["<eos>"] = new { id = "<eos>", ids = new[] { 1 }, tokens = new[] { "<eos>" } } } },
        });
    }

    internal static GemmaTokenizer Create()
    {
        string path = Path.GetTempFileName();
        try { File.WriteAllText(path, FixtureJson()); return new GemmaTokenizer(path); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MergeRankDiffersFromTokenId_AndNewNeighborsAreConsidered()
    {
        var tokenizer = Create();
        Assert.Equal(new long[] { 2, 10, 30, 1 }, tokenizer.Encode("abc"));
        Assert.Equal(new long[] { 24 }, tokenizer.Encode("aaa", false));
        Assert.Equal(new long[] { 23, 23 }, tokenizer.Encode("aaaa", false));
        Assert.Equal(new long[] { 21, 21 }, tokenizer.Encode("abab", false));
    }

    [Fact]
    public void WhitespaceAndUnicodeArePreserved_AndFallbackUsesUtf8()
    {
        var tokenizer = Create();
        Assert.Equal(new long[] { 2, 13, 13, 14, 15, 10, 13, 1 }, tokenizer.Encode("  \t\na "));
        Assert.Equal(new long[] { 2, 16, 1 }, tokenizer.Encode("é"));
        Assert.Equal(new long[] { 2, 201, 304, 229, 1 }, tokenizer.Encode("e\u0301"));
        Assert.Equal(new long[] { 2, 340, 259, 267, 270, 1 }, tokenizer.Encode("🧪"));
        Assert.Equal(new long[] { 2, 100, 1 }, tokenizer.Encode("\0"));
    }

    [Fact]
    public void AddedTokensSplitBpe_AndAutomaticSpecialsCanBeDisabled()
    {
        var tokenizer = Create();
        Assert.Equal(new long[] { 2, 10, 4, 11, 1 }, tokenizer.Encode("a<mask>b"));
        Assert.Equal(new long[] { 2, 2, 1, 1 }, tokenizer.Encode("<bos><eos>"));
        Assert.Equal(new long[] { 2, 1 }, tokenizer.Encode(""));
        Assert.Empty(tokenizer.Encode("", false));
    }

    [Fact]
    public void BatchRightPads_AndAcceptsExactLimitWithoutTruncation()
    {
        var tokenizer = Create();
        var batch = tokenizer.EncodeBatch(new[] { "", "a c" }, 5);
        Assert.Equal(2, batch.BatchSize);
        Assert.Equal(5, batch.SequenceLength);
        Assert.Equal(new[] { 2, 5 }, batch.TokenCounts);
        Assert.Equal(new long[] { 2, 1, 0, 0, 0, 2, 10, 13, 12, 1 }, batch.InputIds);
        Assert.Equal(new long[] { 1, 1, 0, 0, 0, 1, 1, 1, 1, 1 }, batch.AttentionMask);
        Assert.Throws<ArgumentException>(() => tokenizer.EncodeBatch(new[] { "a c" }, 4));
        Assert.Throws<ArgumentException>(() => tokenizer.EncodeBatch(Array.Empty<string>(), 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => tokenizer.EncodeBatch(new[] { "" }, 1));
    }

    [Fact]
    public void InvalidUtf16FailsExplicitly()
    {
        var tokenizer = Create();
        foreach (string text in new[] { new string((char)0xD800, 1), "x" + (char)0xDFFF, "<mask>" + (char)0xD800 })
            Assert.Throws<ArgumentException>(() => tokenizer.Encode(text));
    }

    [Fact]
    public void OutputsAreOwned_AndSharedTokenizerIsConcurrent()
    {
        var tokenizer = Create();
        long[] expected = tokenizer.Encode("a<mask>abc 🧪");
        Parallel.For(0, 32, _ => Assert.Equal(expected, tokenizer.Encode("a<mask>abc 🧪")));
        long[] changed = tokenizer.Encode("a<mask>abc 🧪");
        Array.Fill(changed, -1);
        Assert.Equal(expected, tokenizer.Encode("a<mask>abc 🧪"));
        Assert.Equal(64, tokenizer.TokenizerSha256.Length);
    }

    [Fact]
    public void UnsupportedPipelineIsRejected()
    {
        string path = Path.GetTempFileName();
        try
        {
            string json = FixtureJson().Replace("\"byte_fallback\":true", "\"byte_fallback\":false", StringComparison.Ordinal);
            Assert.NotEqual(FixtureJson(), json);
            File.WriteAllText(path, json);
            Assert.Throws<NotSupportedException>(() => new GemmaTokenizer(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ByteFallbackUsesNormalizedSpace_WhenMetaspaceIsAbsentFromVocabulary()
    {
        var json = JsonNode.Parse(FixtureJson())!;
        json["model"]!["vocab"]!.AsObject().Remove("▁");
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json.ToJsonString());
            Assert.Equal(new long[] { 2, 326, 250, 229, 1 }, new GemmaTokenizer(path).Encode(" "));
        }
        finally { File.Delete(path); }
    }
}

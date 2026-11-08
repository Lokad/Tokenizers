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
    public void BatchRightPadsRowsAndMasksAtTheExactLimit()
    {
        var tokenizer = Create();
        var batch = tokenizer.EncodeBatch(new[] { "", "a c" }, 5);
        Assert.Equal(2, batch.BatchSize);
        Assert.Equal(5, batch.SequenceLength);
        Assert.Equal(new[] { 2, 5 }, batch.TokenCounts);
        Assert.Equal(new long[] { 2, 1, 0, 0, 0, 2, 10, 13, 12, 1 }, batch.InputIds);
        Assert.Equal(new long[] { 1, 1, 0, 0, 0, 1, 1, 1, 1, 1 }, batch.AttentionMask);
    }

    [Fact]
    public void BatchRejectsOverflowWithoutTruncation() =>
        Assert.Throws<ArgumentException>(() => Create().EncodeBatch(new[] { "a c" }, 4));

    [Fact]
    public void BatchRejectsAnEmptyRowCollection() =>
        Assert.Throws<ArgumentException>(() => Create().EncodeBatch(Array.Empty<string>(), 5));

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void BatchLimitMustAccommodateBosAndEos(int maximum) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Create().EncodeBatch(new[] { "" }, maximum));

    [Fact]
    public void InvalidUtf16FailsExplicitly()
    {
        var tokenizer = Create();
        foreach (string text in new[] { new string((char)0xD800, 1), "x" + (char)0xDFFF, "<mask>" + (char)0xD800 })
            Assert.Throws<ArgumentException>(() => tokenizer.Encode(text));
    }

    [Fact]
    public void EncodingOutputsCanBeMutatedIndependently()
    {
        var tokenizer = Create();
        long[] expected = tokenizer.Encode("a<mask>abc 🧪");
        long[] changed = tokenizer.Encode("a<mask>abc 🧪");
        Array.Fill(changed, -1);
        Assert.Equal(expected, tokenizer.Encode("a<mask>abc 🧪"));
    }

    [Fact]
    public void ImmutableEncoderSupportsConcurrentCallers()
    {
        var tokenizer=Create();
        long[] expected=tokenizer.Encode("a<mask>abc 🧪");
        Parallel.For(0,32,_=>Assert.Equal(expected,tokenizer.Encode("a<mask>abc 🧪")));
    }

    [Fact]
    public void AssetHashIdentifiesTheExactTokenizerFile()
    {
        string expected=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(FixtureJson())));
        Assert.Equal(expected,Create().TokenizerSha256);
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

    [Fact]
    public void TokenLimitsUseTheFinalMergedCount()
    {
        var tokenizer = Create();
        Assert.Equal(new long[] { 2,24,1 }, tokenizer.EncodeBatch(new[] { "aaa" }, 3).InputIds);
        Assert.Throws<ArgumentException>(() => tokenizer.EncodeBatch(new[] { "aaaa" }, 3));
    }

    [Fact]
    public void ImpossibleLongInputLimitRejectsBeforeBpeStateAllocation()
    {
        var tokenizer=Create();
        string text = new('a', 200_000);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<ArgumentException>(() => tokenizer.EncodeBatch(new[] { text }, 2));
        long allocation = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocation < 200_000, $"Unexpected allocation before rejecting the token limit: {allocation}");
    }

    [Fact]
    public void AddedTokenMatchingIsLongestAndBypassesSpaceNormalization()
    {
        var json = JsonNode.Parse(FixtureJson())!;
        json["model"]!["vocab"]!["<mask>long"] = 400;
        json["model"]!["vocab"]!["two words"] = 401;
        foreach (var (content,id) in new[] { ("<mask>long",400), ("two words",401) })
            json["added_tokens"]!.AsArray().Add(new JsonObject {
                ["content"] = content, ["id"] = id, ["single_word"] = false, ["lstrip"] = false,
                ["rstrip"] = false, ["normalized"] = false, ["special"] = true
            });
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,json.ToJsonString());
            var tokenizer = new GemmaTokenizer(path);
            Assert.Equal(new long[] { 400,4,401 }, tokenizer.Encode("<mask>long<mask>two words", false));
            Assert.Equal(new long[] { 2,401,1 }, tokenizer.EncodeBatch(new[] { "two words" },3).InputIds);
        }
        finally { File.Delete(path); }
    }
}

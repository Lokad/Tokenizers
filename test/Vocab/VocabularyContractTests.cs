using Lokad.Tokenizers.Exceptions;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Tests.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Vocab.Tests;

public sealed class VocabularyContractTests
{
    [Fact]
    public void VocabularyOwnsItsInputsAndExposesReadOnlyViews()
    {
        var values = new Dictionary<string, long> { ["[UNK]"] = 0, ["a"] = 1 };
        var mapping = new SpecialTokenMap();
        var vocabulary = new BaseVocab(values, mapping);
        values["a"] = 99;
        mapping.UnkToken = "changed";
        vocabulary.SpecialTokenMap.UnkToken = "changed again";
        Assert.Equal(1, vocabulary.TokenToId("a"));
        Assert.Equal("[UNK]", vocabulary.GetUnknownValue());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, long>)vocabulary.Values).Add("b", 2));
        vocabulary.AddExtraIds(1);
        long first = vocabulary.TokenToId("<extra_id_0>");
        vocabulary.AddExtraIds(1);
        vocabulary.AddExtraIds(2);
        vocabulary.AddTokens(new[] { "b", "b", "a" });
        Assert.Equal(first, vocabulary.TokenToId("<extra_id_0>"));
        Assert.Equal(vocabulary.Values.Count, vocabulary.Indices.Count);
        foreach (var entry in vocabulary.Values) Assert.Equal(entry.Key, vocabulary.IdToToken(entry.Value));
        int count = vocabulary.Values.Count;
        Assert.Throws<ArgumentException>(() => vocabulary.AddTokens(new[] { "valid", "" }));
        Assert.Equal(count, vocabulary.Values.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => vocabulary.AddExtraIds(-1));
    }

    [Fact]
    public void XlmDefaultsAreIdenticalThroughEveryAccessPath()
    {
        var vocabulary = new XlmRobertaVocab(new() { ["<s>"] = 0, ["<pad>"] = 1, ["</s>"] = 2, ["<unk>"] = 3, ["<mask>"] = 4 },
            new() { UnkToken = "<unk>" });
        BaseVocab parent = vocabulary;
        IVocab contract = vocabulary;
        Assert.Equal("<pad>", vocabulary.GetPadValue());
        Assert.Equal(vocabulary.GetPadValue(), parent.GetPadValue());
        Assert.Equal(vocabulary.GetPadValue(), contract.GetPadValue());
        Assert.Equal(vocabulary.GetBosValue(), contract.GetBosValue());
        Assert.Equal(vocabulary.GetEosValue(), contract.GetEosValue());
        Assert.Equal(vocabulary.GetMaskValue(), contract.GetMaskValue());
        Assert.Equal(1, contract.SpecialValues[contract.GetPadValue()]);
        Assert.Throws<TokenNotFoundTokenizerException>(() => new XlmRobertaVocab(
            new() { ["<s>"] = 0, ["<pad>"] = 1, ["</s>"] = 2, ["<unk>"] = 3, ["<mask>"] = 4 },
            new() { UnkToken = "missing" }));
    }

    [Theory]
    [InlineData("{\"unk_token\":\"<unk>\",\"pad_token\":\"<pad>\",\"additional_special_tokens\":[\"<extra>\"]}")]
    [InlineData("{\"UnkToken\":\"<unk>\",\"PadToken\":\"<pad>\",\"AdditionalSpecialTokens\":[\"<extra>\"]}")]
    [InlineData("{\"unk_token\":{\"content\":\"<unk>\",\"normalized\":false,\"special\":true},\"pad_token\":\"<pad>\",\"additional_special_tokens\":[{\"content\":\"<extra>\"}]}")]
    public void MappingReaderSupportsItsDocumentedSchemas(string json)
    {
        WithFile(json, path => {
            var mapping = VocabHelper.ReadSpecialTokenMappingFile(path);
            Assert.Equal("<unk>", mapping.UnkToken);
            Assert.Equal("<pad>", mapping.PadToken);
            Assert.Equal(new[] { "<extra>" }, mapping.AdditionalSpecialTokens);
        });
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"typo_token\":\"x\"}")]
    [InlineData("{\"unk_token\":null}")]
    [InlineData("{\"unk_token\":\"\"}")]
    [InlineData("{\"unk_token\":\"x\",\"UnkToken\":\"y\"}")]
    public void InvalidMappingFailsExplicitly(string json) =>
        WithFile(json, path => Assert.Throws<InvalidDataException>(() => VocabHelper.ReadSpecialTokenMappingFile(path)));

    [Fact]
    public void MappingFlagsAreNeverSilentlyIgnored() =>
        WithFile("{\"unk_token\":{\"content\":\"x\",\"single_word\":true}}", path =>
            Assert.Throws<NotSupportedException>(() => VocabHelper.ReadSpecialTokenMappingFile(path)));

    [Fact]
    public void CustomXlmMappingKeepsModelIdsAndDefaults()
    {
        WithFile("{\"pad_token\":\"<custom_pad>\",\"mask_token\":{\"content\":\"<custom_mask>\"}}", path => {
            var tokenizer = new XLMRobertaTokenizer(TestUtils.XlmRobertaFixture(), false, path);
            Assert.Equal(1, tokenizer.Vocab.TokenToId("<custom_pad>"));
            Assert.Equal(3, tokenizer.Vocab.TokenToId("<unk>"));
            Assert.Equal("<s>", tokenizer.Vocab.GetClsValue());
            Assert.Equal(new long[] { 0, 153, 2 }, tokenizer.Encode("…", null, 128, TruncationStrategy.LongestFirst, 0).TokenIds);
            Assert.Contains("<custom_mask>", tokenizer.Tokenize("é<custom_mask>"));
        });
    }

    static void WithFile(string content, Action<string> action)
    {
        string path = Path.GetTempFileName();
        try { File.WriteAllText(path, content); action(path); }
        finally { File.Delete(path); }
    }
}

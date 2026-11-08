using System.Globalization;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

[CollectionDefinition("XLM-R")]
public sealed class XlmCollection : ICollectionFixture<XlmTokenizerFixture> { }

public sealed class XlmTokenizerFixture
{
    public XLMRobertaTokenizer Original { get; }
    public XLMRobertaTokenizer Lowercase { get; }

    public XlmTokenizerFixture()
    {
        string path = TestUtils.XlmRobertaFixture();
        Original = new(path, false);
        Lowercase = Original.WithLowerCase(true);
    }
}

[Collection("XLM-R")]
public sealed class TokenizerRegressionTests(XlmTokenizerFixture fixture)
{
    [Theory]
    [InlineData("é<mask>")]
    [InlineData("🧪<mask>")]
    [InlineData("é<mask>🧪<mask>é")]
    [InlineData("<mask><mask>")]
    public void SpecialTokensAreRecognizedAfterUnicode(string text)
    {
        var result = fixture.Original.TokenizeWithOffsets(text);
        int expected = text.Split("<mask>", StringSplitOptions.None).Length - 1;
        Assert.Equal(expected, result.Tokens.Count(t => t == "<mask>"));
        foreach (int index in Enumerable.Range(0, result.Tokens.Count).Where(i => result.Tokens[i] == "<mask>"))
        {
            Assert.Equal(Mask.Special, result.Masks[index]);
            Assert.Equal(6u, result.Offsets[index]!.End - result.Offsets[index]!.Begin);
        }
    }

    [Theory]
    [InlineData("🧪ABC", "🧪abc")]
    [InlineData("𩸽XYZ", "𩸽xyz")]
    [InlineData("I", "i")]
    public void PublicLowercasePreservesContentAndIgnoresCulture(string original, string expected)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            long[] expectedIds = fixture.Original.Encode(expected, null, 128, TruncationStrategy.LongestFirst, 0).TokenIds.ToArray();
            foreach (string name in new[] { "en-US", "tr-TR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                Assert.Equal(expectedIds, fixture.Lowercase.Encode(original, null, 128, TruncationStrategy.LongestFirst, 0).TokenIds);
            }
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Theory]
    [InlineData("Hello, world!")]
    [InlineData("Café 東京!")]
    public void XlmDecodeUsesSentencePieceRules(string text)
    {
        var ids = fixture.Original.Encode(text, null, 128, TruncationStrategy.LongestFirst, 0).TokenIds;
        Assert.Equal(text, fixture.Original.Decode(ids, true, false));
        Assert.Equal(text, fixture.Original.ConvertTokensToString(
            ids.Where(id => !fixture.Original.Vocab.SpecialIndices.ContainsKey(id)).Select(fixture.Original.Vocab.IdToToken).ToList()));
    }

    [Fact]
    public void GenericDecodeKeepsWordBoundaries()
    {
        var vocab = new BaseVocab(new() { ["[UNK]"] = 0, ["hello"] = 1, ["world"] = 2 }, new());
        var tokenizer = new BaseTokenizer<BaseVocab>(vocab, false, false);
        Assert.Equal("hello world", tokenizer.Decode(new() { 1, 2 }, true, false));
    }

    [Fact]
    public void SentencePieceDecodingUsesPinnedBoundaryWhitespace()
    {
        Assert.Equal("\uFFFE🧪",fixture.Original.ConvertTokensToString(["\u0085▁","\uFFFE🧪","\u2028"]));
    }
}

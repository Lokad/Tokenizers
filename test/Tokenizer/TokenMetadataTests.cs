using System.Text;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class TokenMetadataTests
{
    static BaseTokenizer<BaseVocab> Basic(bool lower = false, bool accents = false) => new(
        new BaseVocab(new() { ["[UNK]"] = 0, ["<mask>"] = 1, ["<mask>long"] = 2, ["two words"] = 3 },
            new() { MaskToken = "<mask>", AdditionalSpecialTokens = new() { "<mask>long", "two words" } }), lower, accents);

    [Fact]
    public void CloneOwnsOffsetsAndScalarPositions()
    {
        var original = new Token("🧪abc");
        Assert.Equal(4, original.ReferenceOffsets.Count);
        var copy = original.Clone();
        copy.Offset.Begin = 10;
        Assert.Equal(0u, original.Offset.Begin);
        var offsets = new uint[] { 5, 6 };
        var token = new Token("ab", offsets);
        offsets[0] = 99;
        Assert.Equal(5u, token.ReferenceOffsets[0]);
    }

    [Theory]
    [InlineData("ﬁx", "fix", new uint[] { 8, 8, 9 })]
    [InlineData("éx", "éx", new uint[] { 8, 10 })]
    [InlineData("가x", "가x", new uint[] { 8, 10 })]
    [InlineData("🧪ﬁ", "🧪fi", new uint[] { 8, 9, 9 })]
    public void NormalizationKeepsOrigins(string input, string expected, uint[] starts)
    {
        var token = new Token(input, Enumerable.Range(8, input.EnumerateRunes().Count()).Select(i => (uint)i).ToArray());
        Basic().DecomposeNfkc(token);
        Assert.Equal(expected, token.Text);
        Assert.Equal(starts, token.ReferenceOffsets);
        Assert.Equal(Enumerable.Range(8, input.EnumerateRunes().Count()).Select(i => (uint)i), TokenAlignment.SourcePositions(token));
        Assert.Equal((uint)(8 + input.EnumerateRunes().Count()), token.Offset.End);
    }

    [Fact]
    public void ReorderedCombiningMarksRetainTheirOwnPositions()
    {
        var token = new Token("a\u0301\u0327z");
        Basic().DecomposeNfkc(token);
        Assert.Equal("á\u0327z", token.Text);
        Assert.Equal(new uint[] { 0, 2, 3 }, token.ReferenceOffsets);
        Assert.Equal(new uint[] { 0, 1 }, token.ScalarReferences![0]);
        Assert.Equal(new uint[] { 2 }, token.ScalarReferences[1]);
    }

    [Fact]
    public void SplitsKeepScalarOffsetsAndSpecialMasks()
    {
        var result = Basic().TokenizeWithOffsets("hello 🧪world! عربي 中𩸽");
        Assert.Equal(new[] { "hello", "🧪world", "!", "عربي", "中", "𩸽" }, result.Tokens);
        Assert.Equal(new Offset?[] { new(0,5), new(6,12), new(12,13), new(14,18), new(19,20), new(20,21) }, result.Offsets);
        Assert.Equal(new[] { Mask.None, Mask.None, Mask.Punctuation, Mask.None, Mask.CJK, Mask.CJK }, result.Masks);
        var special = Basic().TokenizeWithOffsets("é<mask>long🧪two words<mask>");
        Assert.Equal(new[] { "é", "<mask>long", "🧪", "two words", "<mask>" }, special.Tokens);
        Assert.Equal(new[] { Mask.None, Mask.Special, Mask.None, Mask.Special, Mask.Special }, special.Masks);
    }

    [Fact]
    public void CleaningAndAccentRemovalPreserveSourcePositions()
    {
        var result = Basic(true, true).TokenizeWithOffsets("🧪AB\0CéZ");
        Assert.Equal(new[] { "🧪abcez" }, result.Tokens);
        Assert.Equal(new uint[] { 0,1,2,4,5,7 }, result.ReferenceOffsets[0]);
        Assert.Equal(new Offset(0,8), result.Offsets[0]);
        var dotted = new Token("İ");
        TokenizationUtils.Lowercase(dotted);
        Assert.Equal("i\u0307", dotted.Text);
        Assert.Equal(new uint[] { 0,0 }, dotted.ReferenceOffsets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FileConstructorsLoadTheSelectedUnknownMapping(bool mapped)
    {
        string vocabulary=Path.GetTempFileName(),mapping=Path.GetTempFileName();
        try
        {
            string unknown=mapped?"<custom>":"[UNK]";
            File.WriteAllText(vocabulary,unknown+"\nhello\nworld\n");
            File.WriteAllText(mapping,"{\"UnkToken\":\""+unknown+"\"}");
            ITokenizer<BaseVocab> tokenizer=mapped
                ? new BaseTokenizer<BaseVocab>(vocabulary,false,false,mapping)
                : new BaseTokenizer<BaseVocab>(vocabulary,false,false);
            Assert.Equal(new long[]{1,2},tokenizer.Encode("hello world",null,3,TruncationStrategy.LongestFirst,0).TokenIds);
            Assert.Equal(unknown,tokenizer.Vocab.GetUnknownValue());
        }
        finally {File.Delete(vocabulary);File.Delete(mapping);}
    }

    [Fact]
    public void CleanupPreservesOrdinaryDoNotPhrase() => Assert.Equal("I do not agree",Basic().CleanUpTokenization("I do not agree"));
}

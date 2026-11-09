using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Lokad.Tokenizers.Vocab.Tests;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class XlmInputIdentityTests
{
    [Theory]
    [InlineData("＜mask＞", new uint[] { 0,1,2,3,4,5 })]
    [InlineData("<ma\0sk>", new uint[] { 0,1,2,4,5,6 })]
    public void CleaningCannotTurnUnknownRunsIntoSpecialIds(string text, uint[] origins)
    {
        var tokenizer = UnigramFixture.Tokenizer(("▁", -1));
        var result = tokenizer.Encode(text, null, 4, TruncationStrategy.DoNotTruncate, 0);
        Assert.Equal(new long[] { 0,4,3,2 }, result.TokenIds);
        Assert.Equal(Mask.Unknown, result.Mask[2]);
        Assert.Equal((byte)0, result.SpecialTokensMask[2]);
        Assert.Equal(origins, result.ReferenceOffsets[2]);
        Assert.Equal(new long[] { 0,5,2 }, tokenizer.Encode("<mask>", null, 3, TruncationStrategy.DoNotTruncate, 0).TokenIds);
    }

    [Fact]
    public void DictionaryLookupDoesNotOverrideUnknownModelIdentity()
    {
        var tokenizer = UnigramFixture.Tokenizer(("▁", -1));
        var before = tokenizer.Encode("🧪", null, 4, TruncationStrategy.DoNotTruncate, 0);
        tokenizer.Vocab.AddTokens(["🧪"]);
        var after = tokenizer.Encode("🧪", null, 4, TruncationStrategy.DoNotTruncate, 0);
        Assert.Equal(before.TokenIds, after.TokenIds);
        Assert.Equal(new long[] { 0,4,3,2 }, after.TokenIds);
        Assert.Equal(Mask.Unknown, after.Mask[2]);
        Assert.Equal(new uint[] { 0 }, after.ReferenceOffsets[2]);
        Assert.Equal(new[] { tokenizer.Vocab.TokenToId("🧪") }, tokenizer.ConvertTokensToIds(["🧪"]));
        Assert.Equal("🧪", tokenizer.Vocab.IdToToken(tokenizer.Vocab.TokenToId("🧪")));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(" \0")]
    [InlineData("\0 ")]
    [InlineData("\t\0")]
    [InlineData(" \u200B")]
    [InlineData("\uFEFF ")]
    public void EffectiveBlankInputHasOnePolicyAcrossEntriesAndLimits(string text)
    {
        var tokenizer = UnigramFixture.Tokenizer(("▁", -1));
        Assert.Empty(tokenizer.Tokenize(text));
        Assert.Empty(tokenizer.TokenizeWithOffsets(text).Tokens);
        Assert.Empty(tokenizer.TokenizeToTokens(new Token(text)));
        foreach (var strategy in Enum.GetValues<TruncationStrategy>())
        {
            var result = tokenizer.Encode(text, null, 2, strategy, 9);
            Assert.Equal(new long[] { 0,2 }, result.TokenIds);
            Assert.Equal(0, result.NumTruncatedTokens);
            Assert.Empty(result.OverflowingTokens);
            Assert.All(result.ReferenceOffsets, Assert.Empty);
        }
    }

    [Fact]
    public void BlankPolicyPreservesOriginalLiteralsAndContentWhitespace()
    {
        var vocabulary = new XlmRobertaVocab(new() { ["<s>"]=0, [" "]=1, ["</s>"]=2, ["<unk>"]=3, ["<mask>"]=4 },
            new() { UnkToken="<unk>", PadToken=" " });
        var literal = new XLMRobertaTokenizer(vocabulary, new SentencePieceModel(), false);
        Assert.Equal(new long[] { 0,1,2 }, literal.Encode("\0 ", null, 3, TruncationStrategy.DoNotTruncate, 0).TokenIds);
        Assert.Equal(new long[] { 0,2 }, literal.Encode("\t\0", null, 2, TruncationStrategy.DoNotTruncate, 0).TokenIds);
        var ordinary = UnigramFixture.Tokenizer(("▁", -1), ("x", -1));
        foreach (string text in new[] { "x ", "<mask> " })
            Assert.Equal(ordinary.Encode(text, null, 16, TruncationStrategy.DoNotTruncate, 0).TokenIds,
                ordinary.Encode(text+"\0", null, 16, TruncationStrategy.DoNotTruncate, 0).TokenIds);
    }
}

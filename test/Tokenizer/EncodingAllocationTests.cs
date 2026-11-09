using Lokad.Tokenizers.Exceptions;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class EncodingAllocationTests
{
    private class Extended : BaseTokenizer<BaseVocab>
    {
        internal int TemplateCalls;
        internal Extended() : base(new BaseVocab(new() { ["[UNK]"]=0, ["one"]=1 }, new()), false, false) { }
        public override TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets first, TokenIdsWithOffsets? second = null)
        {
            TemplateCalls++;
            var result = base.BuildInputWithSpecialTokens(first, second);
            result.TokenIds.Add(99); result.TokenOffsets.Add(null); result.ReferenceOffsets.Add([]);
            result.Mask.Add(Mask.Special); result.SegmentIds.Add(0); result.SpecialTokensMask.Add(1);
            return result;
        }
    }

    private sealed class ExtendedXlm : XLMRobertaTokenizer
    {
        internal int TemplateCalls;
        internal ExtendedXlm() : base(new XlmRobertaVocab(new() {
            ["<s>"]=0, ["<pad>"]=1, ["</s>"]=2, ["<unk>"]=3, ["<mask>"]=4
        }, new() { UnkToken="<unk>" }), new SentencePieceModel(), false) { }
        public override TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets first, TokenIdsWithOffsets? second = null)
        {
            TemplateCalls++;
            var result=base.BuildInputWithSpecialTokens(first,second);
            result.TokenIds.Add(99); result.TokenOffsets.Add(null); result.ReferenceOffsets.Add([]);
            result.Mask.Add(Mask.Special); result.SegmentIds.Add(0); result.SpecialTokensMask.Add(1);
            return result;
        }
    }

    [Fact]
    public void DerivedXlmTemplatesKeepTheirVirtualHookAndExtraSpecialMinimum()
    {
        var tokenizer = new ExtendedXlm();
        Assert.Equal(new long[] { 0,2,99 }, tokenizer.Encode("", null, 3, TruncationStrategy.LongestFirst, 0).TokenIds);
        Assert.Equal(2, tokenizer.TemplateCalls);
        Assert.Throws<ValueTokenizerException>(() => tokenizer.Encode("", null, 2, TruncationStrategy.LongestFirst, 0));
    }

    [Fact]
    public void DerivedTemplatesKeepTheirVirtualHookAndMinimum()
    {
        var tokenizer = new Extended();
        Assert.Equal(new long[] { 1,99 }, tokenizer.Encode("one", null, 2, TruncationStrategy.LongestFirst, 0).TokenIds);
        Assert.Equal(2, tokenizer.TemplateCalls);
        Assert.Throws<ValueTokenizerException>(() => tokenizer.Encode("", null, 0, TruncationStrategy.LongestFirst, 0));
        var basic = new BaseTokenizer<BaseVocab>(tokenizer.Vocab, false, false);
        Assert.Empty(basic.Encode("", null, 0, TruncationStrategy.LongestFirst, 0).TokenIds);
    }

    [Fact]
    public void EncodeAvoidsPublicIntermediateAndTemplateCopies()
    {
        var tokenizer = new BaseTokenizer<BaseVocab>(new BaseVocab(new() { ["[UNK]"]=0, ["one"]=1 }, new()), false, false);
        string text = string.Join(' ', Enumerable.Repeat("one", 1000));
        // A derived class intentionally exercises the defensive public template route.
        var extended = new Extended();
        tokenizer.Encode(text, null, 2000, TruncationStrategy.LongestFirst, 0);
        extended.Encode(text, null, 2000, TruncationStrategy.LongestFirst, 0);
        long before = GC.GetAllocatedBytesForCurrentThread();
        var fast = tokenizer.Encode(text, null, 2000, TruncationStrategy.LongestFirst, 0);
        long owned = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        var defensive = extended.Encode(text, null, 2000, TruncationStrategy.LongestFirst, 0);
        long copied = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(fast.TokenIds, defensive.TokenIds.Take(1000));
        Assert.True(owned + 50_000 < copied, $"Owned: {owned}, defensive: {copied}");
        var truncated = tokenizer.Encode(text, null, 1, TruncationStrategy.LongestFirst, 0);
        Assert.Single(truncated.TokenIds); Assert.Equal(999, truncated.OverflowingTokens.Count);
        fast.ReferenceOffsets[0][0] = 99; fast.TokenOffsets[0]!.Begin = 99;
        Assert.Equal(new uint[] { 0,1,2 }, truncated.ReferenceOffsets[0]);
        Assert.Equal(new Offset(0,3), truncated.TokenOffsets[0]);
    }
}

using Lokad.Tokenizers.Exceptions;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

[Collection("XLM-R")]
public sealed class XlmEncodingAllocationTests(XlmTokenizerFixture fixture)
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 3)]
    public void ImpossibleBuiltInLimitsRejectBeforeAllocatingInputMetadata(bool paired, int maximum)
    {
        string text = string.Concat(Enumerable.Repeat("hello world ", 3000));
        void Reject() => Assert.Throws<ValueTokenizerException>(() => fixture.Original.Encode(
            text, paired ? text : null, maximum, TruncationStrategy.LongestFirst, 0));
        Reject();
        long before = GC.GetAllocatedBytesForCurrentThread();
        Reject();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated < 32_000, $"Impossible template allocated input metadata: {allocated}");
        Assert.Equal(paired ? new long[] { 0,2,2,2 } : [0,2], fixture.Original.Encode(
            "", paired ? "" : null, maximum + 1, TruncationStrategy.LongestFirst, 0).TokenIds);
    }
}

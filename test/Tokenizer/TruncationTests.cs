using Lokad.Tokenizers.Exceptions;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class TruncationTests
{
    static TokenIdsWithOffsets Sequence(params long[] ids) => new()
    {
        Ids = ids.ToList(), Offsets = ids.Select(i => (Offset?)new Offset((uint)i, (uint)i + 1)).ToList(),
        ReferenceOffsets = ids.Select(i => new List<uint> { (uint)i }).ToList(), Masks = ids.Select(_ => Mask.None).ToList()
    };

    [Theory]
    [InlineData(TruncationStrategy.OnlyFirst, false, 2, 1, new long[] { 10 }, new long[] { 10,11,12 })]
    [InlineData(TruncationStrategy.LongestFirst, false, 2, 0, new long[] { 10 }, new long[] { 11,12 })]
    [InlineData(TruncationStrategy.OnlyFirst, true, 2, 1, new long[] { 10,20,21 }, new long[] { 10,11,12 })]
    [InlineData(TruncationStrategy.OnlySecond, true, 1, 1, new long[] { 10,11,12,20 }, new long[] { 20,21 })]
    [InlineData(TruncationStrategy.LongestFirst, true, 2, 0, new long[] { 10,20,21 }, new long[] { 11,12 })]
    [InlineData(TruncationStrategy.LongestFirst, true, 3, 1, new long[] { 10,20 }, new long[] { 10,21,11,12 })]
    [InlineData(TruncationStrategy.OnlySecond, true, 2, 9, new long[] { 10,11,12 }, new long[] { 20,21 })]
    public void StrategiesKeepAllParallelMetadataAligned(TruncationStrategy strategy, bool paired, int remove, int stride,
        long[] retained, long[] overflow)
    {
        var first = Sequence(10,11,12);
        var second = paired ? Sequence(20,21) : null;
        var (a,b,removed,offsets) = TokenizationUtils.TruncateSequences(first, second, remove, strategy, stride);
        Assert.Equal(retained, a.Ids.Concat(b?.Ids ?? []));
        Assert.Equal(overflow, removed);
        Assert.Equal(removed.Select(i => new Offset((uint)i,(uint)i+1)), offsets);
        foreach (var sequence in new[] { a,b }.OfType<TokenIdsWithOffsets>())
        {
            Assert.Equal(sequence.Ids.Select(i => (uint)i), sequence.Offsets.Select(o => o!.Begin));
            Assert.Equal(sequence.Ids.Select(i => (uint)i), sequence.ReferenceOffsets.Select(r => Assert.Single(r)));
            Assert.Equal(sequence.Ids.Count, sequence.Masks.Count);
        }
    }

    [Fact]
    public void InvalidArgumentsAndImpossibleTruncationDoNotMutateInputs()
    {
        var input = Sequence(10,11);
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenizationUtils.TruncateSequences(input,null,-1,TruncationStrategy.OnlyFirst,0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenizationUtils.TruncateSequences(input,null,1,TruncationStrategy.OnlyFirst,-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenizationUtils.TruncateSequences(input,null,0,(TruncationStrategy)99,0));
        foreach (var strategy in Enum.GetValues<TruncationStrategy>())
            Assert.Throws<ValueTokenizerException>(() => TokenizationUtils.TruncateSequences(input,null,3,strategy,0));
        Assert.Throws<ValueTokenizerException>(() => TokenizationUtils.TruncateSequences(input,null,1,TruncationStrategy.OnlySecond,0));
        Assert.Throws<ValueTokenizerException>(() => TokenizationUtils.TruncateSequences(input,Sequence(20),1,TruncationStrategy.DoNotTruncate,0));
        Assert.Equal(new long[] { 10,11 },input.Ids);
        input.Masks.Clear();
        Assert.Throws<ArgumentException>(() => TokenizationUtils.TruncateSequences(input,null,1,TruncationStrategy.OnlyFirst,0));
        Assert.Equal(new long[] { 10,11 },input.Ids);
    }
}

using Lokad.Tokenizers.Exceptions;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class TruncationTests
{
    [Theory]
    [InlineData(new long[] { 10 }, new long[] { 20,21,22,23 }, 1, 2, new long[] { 21,22,23 })]
    [InlineData(new long[] { 10,11,12,13 }, new long[] { 20 }, 1, 2, new long[] { 11,12,13 })]
    [InlineData(new long[] { }, new long[] { 20,21,22 }, 1, 9, new long[] { 20,21,22 })]
    [InlineData(new long[] { 10,11,12 }, new long[] { }, 1, 9, new long[] { 10,11,12 })]
    [InlineData(new long[] { 10,11,12 }, new long[] { 20,21 }, 3, 9, new long[] { 10,21,11,12 })]
    public void LongestFirstStrideUsesTheTrimmedSideOrFirstSideForMixedRemovals(
        long[] first, long[] second, int remove, int stride, long[] expected)
    {
        var (_, _, overflow, offsets) = TokenizationUtils.TruncateSequences(
            Sequence(first), Sequence(second), remove, TruncationStrategy.LongestFirst, stride);
        Assert.Equal(expected, overflow);
        Assert.Equal(expected.Select(i => new Offset((uint)i, (uint)i + 1)), offsets);
    }

    [Fact]
    public void TruncationKeepsMultiOriginReferencesWithTheirTokens()
    {
        var input = new TokenIdsWithOffsets { Ids=[10,11], Offsets=[new(0,2),new(2,4)],
            ReferenceOffsets=[[0,1],[2,3]], Masks=[Mask.None,Mask.None] };
        var result=TokenizationUtils.TruncateSequences(input,null,1,TruncationStrategy.OnlyFirst,0);
        Assert.Equal(new long[]{10},result.Item1.Ids);
        Assert.Equal(new uint[][]{[0,1]},result.Item1.ReferenceOffsets.Select(r=>r.ToArray()));
        Assert.Equal(new long[]{11},result.Item3);
        Assert.Equal(new Offset?[]{new(2,4)},result.Item4);
    }
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

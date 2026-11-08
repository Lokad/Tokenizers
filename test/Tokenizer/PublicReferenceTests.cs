using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

[Collection("XLM-R")]
public sealed class PublicReferenceTests(XlmTokenizerFixture fixture)
{
    [Fact]
    public void CompatibilityExpansionKeepsOriginsAcrossOutputPieces()
    {
        var result=fixture.Original.Encode("\uFDFA",null,64,TruncationStrategy.LongestFirst,0);
        Assert.Equal(new long[]{0,16509,1423,4472,8756,2},result.TokenIds);
        Assert.Equal(new uint[][]{[],[0],[0],[0],[0],[]},result.ReferenceOffsets.Select(r=>r.ToArray()));
    }

    [Fact]
    public void EncodingKeepsEveryComposingSourcePosition()
    {
        var result=fixture.Original.Encode("e\u0301",null,64,TruncationStrategy.LongestFirst,0);
        Assert.Equal(new long[]{0,393,2},result.TokenIds);
        Assert.Equal(new uint[][]{[],[0,1],[]},result.ReferenceOffsets.Select(r=>r.ToArray()));
        Assert.Equal(new Offset?[]{null,new(0,2),null},result.TokenOffsets);
    }

    [Fact]
    public void PairReferencesKeepSequenceBOrigins()
    {
        var result=fixture.Original.Encode("e\u0301","e\u0301",64,TruncationStrategy.LongestFirst,0);
        Assert.Equal(new long[]{0,393,2,2,393,2},result.TokenIds);
        Assert.Equal(new uint[][]{[],[0,1],[],[],[0,1],[]},result.ReferenceOffsets.Select(r=>r.ToArray()));
        Assert.Equal(new byte[]{0,0,0,1,1,1},result.SegmentIds);
    }

    [Fact]
    public void PairTruncationPreservesSequenceBCompositionReferences()
    {
        var result=fixture.Original.Encode("e\u0301","e\u0301",5,TruncationStrategy.LongestFirst,0);
        Assert.Equal(new long[]{0,2,2,393,2},result.TokenIds);
        Assert.Equal(new uint[][]{[],[],[],[0,1],[]},result.ReferenceOffsets.Select(r=>r.ToArray()));
        Assert.Equal(1,result.NumTruncatedTokens);
    }

    [Fact]
    public void FusedSupplementaryUnknownsUseScalarPositions()
    {
        var result=fixture.Original.Encode("🧪🧪",null,64,TruncationStrategy.LongestFirst,0);
        Assert.Equal(new long[]{0,6,3,2},result.TokenIds);
        Assert.Equal(new uint[][]{[],[0],[0,1],[]},result.ReferenceOffsets.Select(r=>r.ToArray()));
        Assert.Equal(new Offset(0,2),result.TokenOffsets[2]);
    }
}

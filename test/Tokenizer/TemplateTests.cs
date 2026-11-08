using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class TemplateTests
{
    static XlmRobertaVocab Vocabulary() => new(new() {
        ["<s>"]=0,["<pad>"]=1,["</s>"]=2,["<unk>"]=3,["<mask>"]=4
    },new() { UnkToken="<unk>" });

    static TokenIdsWithOffsets Sequence(params long[] ids) => new() {
        Ids=ids.ToList(), Offsets=ids.Select((_,i)=>(Offset?)new Offset((uint)i,(uint)i+1)).ToList(),
        ReferenceOffsets=ids.Select((_,i)=>new List<uint>{(uint)i}).ToList(), Masks=ids.Select(_=>Mask.None).ToList()
    };

    [Fact]
    public void XlmPairTemplateHasExactIdsAndSegments()
    {
        var tokenizer=new XLMRobertaTokenizer(Vocabulary(),new SentencePieceModel(),false);
        var a=Sequence(101,102); var b=Sequence(201,202);
        var result=tokenizer.BuildInputWithSpecialTokens(a,b);
        Assert.Equal(new long[]{0,101,102,2,2,201,202,2},result.TokenIds);
        Assert.Equal(new byte[]{0,0,0,0,1,1,1,1},result.SegmentIds);
        Assert.Equal(new byte[]{1,0,0,1,1,0,0,1},result.SpecialTokensMask);
        Assert.Equal(new Offset?[]{null,new(0,1),new(1,2),null,null,new(0,1),new(1,2),null},result.TokenOffsets);
        Assert.Equal(new[]{Mask.Special,Mask.None,Mask.None,Mask.Special,Mask.Special,Mask.None,Mask.None,Mask.Special},result.Mask);
        Assert.Equal(new uint[][] { [],[0],[1],[],[],[0],[1],[] }, result.ReferenceOffsets.Select(r => r.ToArray()));
        Assert.Equal(new long[]{101,102},a.Ids);
        Assert.Equal(new long[]{201,202},b.Ids);
    }

    [Fact]
    public void XlmSingleTemplateHasExactIdsAndSegments()
    {
        var tokenizer=new XLMRobertaTokenizer(Vocabulary(),new SentencePieceModel(),false);
        var result=tokenizer.BuildInputWithSpecialTokens(Sequence(101,102));
        Assert.Equal(new long[]{0,101,102,2},result.TokenIds);
        Assert.Equal(new byte[]{0,0,0,0},result.SegmentIds);
        Assert.Equal(new byte[]{1,0,0,1},result.SpecialTokensMask);
    }

    [Fact]
    public void BasicPairKeepsSequenceOwnershipAndHasNoInsertedTokens()
    {
        var tokenizer=new BaseTokenizer<XlmRobertaVocab>(Vocabulary(),false,false);
        var result=tokenizer.BuildInputWithSpecialTokens(Sequence(101,102),Sequence(201));
        Assert.Equal(new long[]{101,102,201},result.TokenIds);
        Assert.Equal(new byte[]{0,0,1},result.SegmentIds);
        Assert.Equal(new byte[]{0,0,0},result.SpecialTokensMask);
        Assert.Equal(new uint[][] { [0],[1],[0] }, result.ReferenceOffsets.Select(r => r.ToArray()));
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void PublicBuildersOwnMutableMetadataInBothDirections(bool xlm, bool second)
    {
        BaseTokenizer<XlmRobertaVocab> tokenizer = xlm
            ? new XLMRobertaTokenizer(Vocabulary(),new SentencePieceModel(),false)
            : new BaseTokenizer<XlmRobertaVocab>(Vocabulary(),false,false);
        var a=Sequence(101,102); var b=Sequence(201,202);
        var input=second?b:a;
        var result=tokenizer.BuildInputWithSpecialTokens(a,b);
        int position=xlm?(second?5:1):(second?2:0);
        result.ReferenceOffsets[position][0]=99;
        result.TokenOffsets[position]!.Begin=98;
        Assert.Equal(new uint[]{0},input.ReferenceOffsets[0]);
        Assert.Equal(new Offset(0,1),input.Offsets[0]);
        input.ReferenceOffsets[1][0]=77;
        input.Offsets[1]!.End=88;
        Assert.Equal(new uint[]{1},result.ReferenceOffsets[position+1]);
        Assert.Equal(new Offset(1,2),result.TokenOffsets[position+1]);
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void PublicBuildersRejectMisalignedMetadata(bool xlm,bool second)
    {
        BaseTokenizer<XlmRobertaVocab> tokenizer=xlm
            ? new XLMRobertaTokenizer(Vocabulary(),new SentencePieceModel(),false)
            : new BaseTokenizer<XlmRobertaVocab>(Vocabulary(),false,false);
        var malformed=new TokenIdsWithOffsets { Ids=[42] };
        Assert.Throws<ArgumentException>(()=>tokenizer.BuildInputWithSpecialTokens(second?Sequence(10):malformed,second?malformed:null));
        Assert.Equal(new long[]{42},malformed.Ids);
    }
}

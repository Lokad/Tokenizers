using System.Text;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Vocab.Tests;

public sealed class NodeMappingTests
{
    [Fact]
    public void SingleScalarCompositionCanBeReconstructedFromLegacyPublicValues()
    {
        var token=new Token("e\u0301");
        TokenizationUtils.DecomposeNfkc(token);
        var model=new SentencePieceModel();
        model.Root.Children[new Rune('é')]=new TrieNode("é") { End=true,Index=3,Score=-1 };
        var node=model.DecodeForwardTokenRef(token)[1]!;
        var copy=new Node(node.Text,node.Score,node.Index,node.Start,node.End,node.ReferenceOffsets);
        var parsed=Assert.Single(model.ParseNodesToTokens([copy]));
        Assert.Equal(new uint[] { 0,1 },TokenAlignment.SourcePositions(parsed));
        var edited=node.ReferenceOffsets;
        edited[0]=99;
        Assert.Equal(new uint[] { 0,1 },node.ReferenceOffsets);
        Assert.Equal(new uint[] { 0,1 },node.ScalarReferences[0]);
    }

    [Fact]
    public void RichPublicMappingRoundTripsExpansionCompositionAndUnknownFusion()
    {
        uint[][] input=[[0],[0],[1,2]];
        var node=Node.FromScalarReferences("fié",-1,3,0,3,input);
        input[0][0]=99;
        var copy=Node.FromScalarReferences(node.Text,node.Score,node.Index,node.Start,node.End,node.ScalarReferences);
        var model=new SentencePieceModel();
        var parsed=Assert.Single(model.ParseNodesToTokens([copy]));
        Assert.Equal(new uint[][] { [0],[0],[1,2] },TokenAlignment.References(parsed));
        Assert.Equal(new uint[] { 0,1,2 },node.ReferenceOffsets);
        Assert.Throws<NotSupportedException>(()=>((IList<uint>)node.ScalarReferences[0])[0]=9);
        var unknown=Node.FromScalarReferences("🧪",-2,0,3,4,new uint[][] { [4] });
        var unknownCopy=Node.FromScalarReferences("é",-3,0,4,5,new uint[][] { [5,6] });
        var fused=Assert.Single(model.ParseNodesToTokens([unknown,unknownCopy]));
        Assert.Equal("🧪é",fused.Text);
        Assert.Equal(Mask.Unknown,fused.Mask);
        Assert.Equal(new uint[] { 4,5,6 },TokenAlignment.SourcePositions(fused));
    }
}

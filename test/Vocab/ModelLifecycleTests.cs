using System.Runtime.CompilerServices;
using System.Text;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Vocab.Tests;

public sealed class ModelLifecycleTests
{
    [Fact]
    public void ValueInspectionAndCompletedEditsReuseSnapshotsForConcurrentReaders()
    {
        var pieces=Enumerable.Range(0,8192).Select(i=>($"piece-{i:D5}",-1f)).Prepend(("aa",-0.5f)).Prepend(("a",-1f)).ToArray();
        var model=SentencePieceModel.FromModel(UnigramFixture.Model(pieces));
        var token=new Token("aa");
        long Measure()
        {
            model.DecodeForwardTokenRef(token);
            long start=GC.GetAllocatedBytesForCurrentThread();
            model.DecodeForwardTokenRef(token);
            return GC.GetAllocatedBytesForCurrentThread()-start;
        }
        long initial=Measure();
        Assert.Contains(model.EnumeratePieces(),p=>p.Text == "aa");
        Assert.True(model.IsFrozen);
        Assert.InRange(Measure(),0,initial+512);
        var oldRoot=model.Root;
        Assert.False(model.IsFrozen);
        oldRoot.Children[new Rune('a')].Children[new Rune('a')].Score=-3;
        model.Freeze();
        Assert.True(model.IsFrozen);
        oldRoot.Children[new Rune('a')].Children[new Rune('a')].Score=100;
        Assert.InRange(Measure(),0,initial+512);
        Parallel.For(0,16,_=>Assert.Equal(new long[] { 3,3 },model.DecodeBackward(model.DecodeForwardTokenRef(token).ToArray()).Select(n=>n.Index)));
        long before=GC.GetAllocatedBytesForCurrentThread();
        model.Freeze();
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        var current=model.CommonPrefixSearch("aa").Last();
        current.Score=100;
        Assert.False(model.IsFrozen);
        Assert.Equal(4,Assert.Single(model.DecodeBackward(model.DecodeForwardTokenRef(token).ToArray())).Index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (SentencePieceModel Model,WeakReference OldPiece) ReplaceRoot()
    {
        var model=SentencePieceModel.FromModel(UnigramFixture.Model(("a",-1),("aa",-0.5f)));
        var oldPiece=model.Root.Children[new Rune('a')];
        var reference=new WeakReference(oldPiece);
        model.Root=new TrieNode("");
        return (model,reference);
    }

    [Fact]
    public void ReplacedRootDoesNotRetainAnObsoleteMatcher()
    {
        var pair=ReplaceRoot();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(pair.OldPiece.IsAlive);
        GC.KeepAlive(pair.Model);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomChildEqualityAgreesWithPrefixSearchBeforeAndAfterFreeze(bool freeze)
    {
        var model=new SentencePieceModel();
        var a=new TrieNode("a") { End=true,Index=3,Score=-1 };
        a.Children.Add(new Rune('b'),new TrieNode("ab") { End=true,Index=4,Score=-0.5f });
        model.Root.Children=new(new FoldAscii()) { [new Rune('a')]=a };
        Assert.Equal(4,model.CommonPrefixSearch("Ab").Last().Index);
        if (freeze) model.Freeze();
        List<Node> Decode(string text)=>model.DecodeBackward(model.DecodeForwardTokenRef(new Token(text)).ToArray());
        Assert.Equal(3,Assert.Single(Decode("A")).Index);
        Assert.Equal(4,Assert.Single(Decode("Ab")).Index);
        Assert.Equal(new long[] { 3,0 },Decode("AB").Select(n=>n.Index));
        Assert.Equal(3,Assert.Single(Decode("a")).Index);
    }

    private sealed class FoldAscii : IEqualityComparer<Rune>
    {
        private static int Code(Rune rune)=>rune.Value is >=65 and <=90 ? rune.Value+32 : rune.Value;
        public bool Equals(Rune left,Rune right)=>Code(left)==Code(right);
        public int GetHashCode(Rune rune)=>Code(rune);
    }
}

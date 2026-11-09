using System.Collections.Concurrent;
using System.Text;
using Xunit;

namespace Lokad.Tokenizers.Vocab.Tests;

public sealed class VocabularyBoundaryTests
{
    [Fact]
    public void ConstructorBuildsBothOwnedViewsWithoutAnExtraIdSet()
    {
        var source = Enumerable.Range(0, 100_000).ToDictionary(i => "v"+i, i => (long)i);
        source["[UNK]"] = 100_000;
        _ = new BaseVocab(source, new());
        long before = GC.GetAllocatedBytesForCurrentThread();
        var result = new BaseVocab(source, new());
        long allocated = GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.True(allocated < 7_000_000, $"Constructor allocated {allocated} bytes");
        Assert.Equal(source.Count, result.Values.Count);
        Assert.Equal("v99999", result.IdToToken(99999));
        Assert.Contains("IDs must be unique", Assert.Throws<ArgumentException>(() => new BaseVocab(new() { ["[UNK]"]=0, ["a"]=0 }, new())).Message);
    }

    [Fact]
    public void LazyWritersCanCallEachOtherWithoutOwningOuterLocks()
    {
        var a = new BaseVocab(new() { ["[UNK]"]=0 }, new());
        var b = new BaseVocab(new() { ["[UNK]"]=0 }, new());
        using var entered = new CountdownEvent(2);
        using var release = new ManualResetEventSlim(false);
        var errors = new ConcurrentQueue<Exception>();
        IEnumerable<string> Cross(BaseVocab other)
        {
            entered.Signal();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
            other.AddTokens(["inner"]);
            yield return "outer";
        }
        Thread Writer(BaseVocab target, BaseVocab other) => new(() =>
        {
            try { target.AddTokens(Cross(other)); }
            catch (Exception error) { errors.Enqueue(error); }
        }) { IsBackground = true };
        var first = Writer(a,b); var second = Writer(b,a);
        first.Start(); second.Start();
        bool bothEntered = entered.Wait(TimeSpan.FromSeconds(5));
        release.Set();
        bool firstDone = first.Join(5000), secondDone = second.Join(5000);
        Assert.True(bothEntered && firstDone && secondDone, "Lazy vocabulary writers did not complete");
        Assert.Empty(errors);
        foreach (var vocab in new[] { a,b })
        {
            Assert.Equal(3, vocab.Values.Count);
            Assert.Equal("inner", vocab.IdToToken(vocab.TokenToId("inner")));
            Assert.Equal("outer", vocab.IdToToken(vocab.TokenToId("outer")));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FlatDecodingRejectsInvalidBytesWithOrWithoutUtf8Bom(bool bom)
    {
        byte[] prefix = Encoding.UTF8.GetBytes("[UNK]\nkey");
        byte[] bad = [.. (bom ? Encoding.UTF8.GetPreamble() : []), ..prefix, 0xFF, ..Encoding.UTF8.GetBytes("value\n")];
        WithBytes(bad, path => {
            var error = Assert.Throws<InvalidDataException>(() => VocabHelper.ReadFlatFile(path));
            Assert.Contains(path, error.Message);
            Assert.IsType<DecoderFallbackException>(error.InnerException);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void StrictDecodingPreservesValidUnicodeAndBomFormats(int format)
    {
        Encoding encoding = format switch {
            0 => new UTF8Encoding(false), 1 => new UTF8Encoding(true),
            2 => new UnicodeEncoding(false,true), 3 => new UnicodeEncoding(true,true),
            4 => new UTF32Encoding(false,true), _ => new UTF32Encoding(true,true)
        };
        byte[] bytes = [..encoding.GetPreamble(), ..encoding.GetBytes("[UNK]\nkeyé\uFFFDvalue\n")];
        WithBytes(bytes, path => Assert.Equal(1, VocabHelper.ReadFlatFile(path)["keyé\uFFFDvalue"]));
        if (format >= 2)
            WithBytes([..bytes, 0xFF], path => Assert.Throws<InvalidDataException>(() => VocabHelper.ReadFlatFile(path)));
    }

    private static void WithBytes(byte[] bytes, Action<string> check)
    {
        string path = Path.GetTempFileName();
        try { File.WriteAllBytes(path, bytes); check(path); }
        finally { File.Delete(path); }
    }
}

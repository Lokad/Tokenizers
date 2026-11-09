using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab.Tests;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class UnigramOriginTests
{
    [Fact]
    public void DecompositionCanStraddleAnExistingMarkWithoutSharingItsOrigins()
    {
        var tokenizer=UnigramFixture.Tokenizer(("▁q",-1),("\u0F71",-1),("\u0F72",-1),("\u0F74",-1));
        var encoded=tokenizer.Encode("q\u0F72\u0F75",null,16,TruncationStrategy.DoNotTruncate,0);
        Assert.Equal(new long[] { 0,4,5,6,7,2 },encoded.TokenIds);
        Assert.Equal(new uint[][] { [],[0],[2],[1],[2],[] },encoded.ReferenceOffsets.Select(r=>r.ToArray()));
    }
}

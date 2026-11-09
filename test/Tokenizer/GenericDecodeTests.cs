using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class GenericDecodeTests
{
    [Fact]
    public void GenericDecodeKeepsWordBoundaries()
    {
        var vocab = new BaseVocab(new() { ["[UNK]"] = 0, ["hello"] = 1, ["world"] = 2 }, new());
        var tokenizer = new BaseTokenizer<BaseVocab>(vocab, false, false);
        Assert.Equal("hello world", tokenizer.Decode(new() { 1, 2 }, true, false));
    }
}

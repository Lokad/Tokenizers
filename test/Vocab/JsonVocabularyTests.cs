using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Vocab;

public sealed class JsonVocabularyTests
{
    private static void WithJson(string json, Action<string> assertion)
    {
        string path=Path.GetTempFileName();
        try { File.WriteAllText(path,json); assertion(path); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReaderPreservesNonnegativeInt64IdsThroughVocabularyConstruction() => WithJson(
        "{\"[UNK]\":0,\"a\":4294967297}", path => {
            var vocabulary=new BaseVocab(VocabHelper.ReadJsonFile(path),new());
            Assert.Equal(4294967297L,vocabulary.TokenToId("a"));
            Assert.Equal("a",vocabulary.IdToToken(4294967297));
            Assert.Equal(0,vocabulary.TokenToId("missing"));
        });

    [Theory]
    [InlineData("{\"[UNK]\":0,\"a\":1,\"a\":99}","a")]
    [InlineData("{\"[UNK]\":0,\"a\":1,\"b\":1}","ID: 1")]
    [InlineData("{\"a\":-1}","a")]
    [InlineData("{\"a\":1.5}","a")]
    [InlineData("{\"a\":9223372036854775808}","a")]
    [InlineData("{\"a\":null}","a")]
    [InlineData("{\"\":1}","name/ID")]
    [InlineData("null","object")]
    [InlineData("[]","object")]
    [InlineData("{","JSON")]
    [InlineData("{\"\\uD800\":1}","vocabulary")]
    public void InvalidVocabularyJsonFailsWithDataDiagnostics(string json,string diagnostic) =>
        WithJson(json,path => Assert.Contains(diagnostic,Assert.Throws<InvalidDataException>(()=>VocabHelper.ReadJsonFile(path)).Message));

    [Theory]
    [InlineData("{\"unk_token\":\"\\uD800\"}", "unk_token")]
    [InlineData("{\"unk_token\":{\"content\":\"\\uDC00\"}}", "unk_token.content")]
    [InlineData("{\"\\uD800\":\"x\"}", "special_tokens")]
    [InlineData("{\"unk_token\":{\"\\uD800\":false,\"content\":\"x\"}}", "unk_token")]
    public void MalformedMappingStringsIdentifyTheirPath(string json, string path) =>
        WithJson(json, file => Assert.Contains(path, Assert.Throws<InvalidDataException>(() => VocabHelper.ReadSpecialTokenMappingFile(file)).Message));

    [Theory]
    [InlineData("\\uD83E\\uDDEA", "🧪")]
    [InlineData("\\uFFFE", "\uFFFE")]
    public void EscapedValidUnicodeIsPreserved(string escaped, string text)
    {
        WithJson("{\"" + escaped + "\":1}", file => Assert.Equal(1, VocabHelper.ReadJsonFile(file)[text]));
        WithJson("{\"unk_token\":\"" + escaped + "\"}", file => Assert.Equal(text, VocabHelper.ReadSpecialTokenMappingFile(file).UnkToken));
    }
}

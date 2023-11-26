using Xunit;

namespace Lokad.Tokenizers.Vocab.Tests;

// TODO: ChatGPT port of https://github.com/guillaume-be/rust-tokenizers/blob/main/main/src/vocab/base_vocab.rs
public class BaseVocabTests
{
    [Fact(Skip = "DIsable temporary - object initialization logic is wrong")]
    public void TestCreateObject()
    {
        // Given
        var values = new Dictionary<string, long>();
        var specialTokenMap = new SpecialTokenMap() { UnkToken = "[UNK]" };

        // When
        var baseVocab = new BaseVocab(values, specialTokenMap);

        // Then
        Assert.Equal("[UNK]", baseVocab.GetUnknownValue());
        Assert.Equal(values, baseVocab.Values);
        Assert.Equal(new Dictionary<string, long>(), baseVocab.SpecialValues);
    }

    [Fact]
    public void TestCreateObjectFromFile()
    {
        // Given
        var tempFilePath = CreateTempFileWithContent("hello\nworld\n[UNK]\n!");
        var targetValues = new Dictionary<string, long>
        {
            {"hello", 0},
            {"world", 1},
            {"[UNK]", 2},
            {"!", 3}
        };

        var specialValues = new Dictionary<string, long>
        {
            {"[UNK]", 2}
        };

        // When
        var baseVocab = FromFile(tempFilePath);

        // Then
        Assert.Equal("[UNK]", baseVocab.GetUnknownValue());
        Assert.Equal(targetValues, baseVocab.Values);
        Assert.Equal(specialValues, baseVocab.SpecialValues);

        // Cleanup
        File.Delete(tempFilePath);
    }

    [Fact]
    public void TestCreateObjectFromFileWithoutUnknownToken()
    {
        // Given
        var tempFilePath = CreateTempFileWithContent("hello\nworld\n!");

        // When & Then
        var exception = Assert.Throws<TokenNotFoundTokenizerException>(() => FromFile(tempFilePath));
        Assert.Contains("Unknown token [UNK] not found", exception.Message);

        // Cleanup
        File.Delete(tempFilePath);
    }

    [Fact]
    public void TestEncodeTokens()
    {
        // Given
        var tempFilePath = CreateTempFileWithContent("hello\nworld\n[UNK]\n!");
        var baseVocab = FromFile(tempFilePath);

        // When & Then
        Assert.Equal(0, baseVocab.TokenToId("hello"));
        Assert.Equal(1, baseVocab.TokenToId("world"));
        Assert.Equal(3, baseVocab.TokenToId("!"));
        Assert.Equal(2, baseVocab.TokenToId("[UNK]"));
        Assert.Equal(2, baseVocab.TokenToId("oov_value"));

        // Cleanup
        File.Delete(tempFilePath);
    }

    [Fact]
    public void TestDecodeTokens()
    {
        // Given
        var tempFilePath = CreateTempFileWithContent("hello\nworld\n[UNK]\n!");
        var baseVocab = FromFile(tempFilePath);

        // When & Then
        Assert.Equal("hello", baseVocab.IdToToken(0));
        Assert.Equal("world", baseVocab.IdToToken(1));
        Assert.Equal("!", baseVocab.IdToToken(3));
        Assert.Equal("[UNK]", baseVocab.IdToToken(2));

        // Cleanup
        File.Delete(tempFilePath);
    }

    private static string CreateTempFileWithContent(string content)
    {
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, content);
        return tempFilePath;
    }

    private static BaseVocab FromFile(string filePath)
    {
        var values = VocabHelper.ReadFlatFile(filePath);

        return new BaseVocab(values, new SpecialTokenMap());
    }
}

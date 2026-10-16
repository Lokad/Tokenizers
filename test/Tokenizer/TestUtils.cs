namespace Lokad.Tokenizers.Tests.Tokenizer;

internal static class TestUtils
{
    public static string XlmRobertaFixture() => Path.Combine(AppContext.BaseDirectory, "Fixture",
        "xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
}

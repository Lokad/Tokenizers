using Lokad.Tokenizers.Tokenizer;
using Xunit;

// Original code: https://github.com/guillaume-be/rust-tokenizers/blob/main/main/tests/test_xlm_roberta_uncased.rs

namespace Lokad.Tokenizers.Tests;

public class XlmRobertaTests
{
    [Fact]
    void TestUncased()
    {
        // TODO: olegra - load-n-cache from URL: https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model
        var path = "xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model";

        var tokenizer = new XLMRobertaTokenizer(path, false);

        var originalStrings = new []
        {
            "…",
            //"This is a sample sentence to be tokénized",
            //"Wondering how this will get tokenized 🤔 ?",
            //"İs th!s 𩸽 Ϻ Šœ Ugljšić dấu nặng",
            //"   İs th!s    𩸽 Ϻ Šœ   Ugljšić  dấu nặng     ",
            //" � İs th!s �� 𩸽 Ϻ Šœ   Ugljšić  dấu nặng     "
        };

        var expectedResults = new[] 
            {
                new TokenizedInput
                {
                    TokenIds = new List<Int64> { 0, 153, 2 },
                    TokenOffsets = new List<Offset?> { null, new(begin: 0, end: 1), null }
                },
                //new TokenizedInput
                //{
                //    TokenIds = new List<Int64> { 0, 3293, 83, 10, 121413, 149357, 47, 186, 25636, 2746, 29367, 2 },
                //    TokenOffsets = new List<Offset?> {
                //        null,
                //        new( begin: 0, end: 4 ),
                //        new( begin: 4, end: 7 ),
                //        new( begin: 7, end: 9 ),
                //        new( begin: 9, end: 16 ),
                //        new( begin: 16, end: 25 ),
                //        new( begin: 25, end: 28 ),
                //        new( begin: 28, end: 31 ),
                //        new( begin: 31, end: 35 ),
                //        new( begin: 35, end: 38 ),
                //        new( begin: 38, end: 42 ),
                //        null }
                //},
                //new TokenizedInput
                //{
                //    TokenIds = new List<Int64> { 0, 76648, 214, 3642, 903, 1221, 2046, 47, 1098, 29367, 6, 243691, 705, 2 },
                //    TokenOffsets = new List<Offset?> {
                //        null,
                //        new( begin: 0, end: 6 ),
                //        new( begin: 6, end: 9 ),
                //        new( begin: 9, end: 13 ),
                //        new( begin: 13, end: 18 ),
                //        new( begin: 18, end: 23 ),
                //        new( begin: 23, end: 27 ),
                //        new( begin: 27, end: 30 ),
                //        new( begin: 30, end: 33 ),
                //        new( begin: 33, end: 37 ),
                //        new( begin: 37, end: 38 ),
                //        new( begin: 38, end: 39 ),
                //        new( begin: 39, end: 41 ),
                //        null }
                //},
                //new TokenizedInput
                //{
                //    TokenIds = new List<Int64> { 0, 63770, 5675, 38, 7, 6, 3, 6, 3, 3608, 52908, 345, 11016, 170, 36277, 39973, 55315, 2 },
                //    TokenOffsets = new List<Offset?> {
                //        null,
                //        new( begin: 0, end: 2 ),
                //        new( begin: 2, end: 5 ),
                //        new( begin: 5, end: 6 ),
                //        new( begin: 6, end: 7 ),
                //        new( begin: 7, end: 8 ),
                //        new( begin: 8, end: 9 ),
                //        new( begin: 9, end: 10 ),
                //        new( begin: 10, end: 11 ),
                //        new( begin: 11, end: 13 ),
                //        new( begin: 13, end: 14 ),
                //        new( begin: 14, end: 16 ),
                //        new( begin: 16, end: 18 ),
                //        new( begin: 18, end: 19 ),
                //        new( begin: 19, end: 22 ),
                //        new( begin: 22, end: 26 ),
                //        new( begin: 26, end: 31 ),
                //        null }
                //},
                //new TokenizedInput
                //{
                //    TokenIds = new List<Int64> { 0, 6, 6, 63770, 5675, 38, 7, 6, 6, 6, 6, 3, 6, 3, 3608, 52908, 6, 6, 345, 11016,
                //        170, 36277, 6, 39973, 55315, 6, 6, 6, 6, 6, 2 },
                //    TokenOffsets = new List<Offset?> {
                //        null,
                //        new( begin: 0, end: 1 ),
                //        new( begin: 1, end: 2 ),
                //        new( begin: 2, end: 5 ),
                //        new( begin: 5, end: 8 ),
                //        new( begin: 8, end: 9 ),
                //        new( begin: 9, end: 10 ),
                //        new( begin: 10, end: 11 ),
                //        new( begin: 11, end: 12 ),
                //        new( begin: 12, end: 13 ),
                //        new( begin: 13, end: 14 ),
                //        new( begin: 14, end: 15 ),
                //        new( begin: 15, end: 16 ),
                //        new( begin: 16, end: 17 ),
                //        new( begin: 17, end: 19 ),
                //        new( begin: 19, end: 20 ),
                //        new( begin: 20, end: 21 ),
                //        new( begin: 21, end: 22 ),
                //        new( begin: 22, end: 24 ),
                //        new( begin: 24, end: 26 ),
                //        new( begin: 26, end: 27 ),
                //        new( begin: 27, end: 30 ),
                //        new( begin: 30, end: 31 ),
                //        new( begin: 31, end: 35 ),
                //        new( begin: 35, end: 40 ),
                //        new( begin: 40, end: 41 ),
                //        new( begin: 41, end: 42 ),
                //        new( begin: 42, end: 43 ),
                //        new( begin: 43, end: 44 ),
                //        new( begin: 44, end: 45 ),
                //        null }
                //},
                //new TokenizedInput
                //{
                //    TokenIds = new List<Int64> { 0, 6, 63770, 5675, 38, 7, 6, 6, 3, 6, 3, 3608, 52908, 6, 6, 345, 11016, 170, 36277,
                //        6, 39973, 55315, 6, 6, 6, 6, 6, 2 },
                //    TokenOffsets = new List<Offset?> {
                //        null,
                //        new( begin: 0, end: 1 ),
                //        new( begin: 2, end: 5 ),
                //        new( begin: 5, end: 8 ),
                //        new( begin: 8, end: 9 ),
                //        new( begin: 9, end: 10 ),
                //        new( begin: 10, end: 11 ),
                //        new( begin: 13, end: 14 ),
                //        new( begin: 14, end: 15 ),
                //        new( begin: 15, end: 16 ),
                //        new( begin: 16, end: 17 ),
                //        new( begin: 17, end: 19 ),
                //        new( begin: 19, end: 20 ),
                //        new( begin: 20, end: 21 ),
                //        new( begin: 21, end: 22 ),
                //        new( begin: 22, end: 24 ),
                //        new( begin: 24, end: 26 ),
                //        new( begin: 26, end: 27 ),
                //        new( begin: 27, end: 30 ),
                //        new( begin: 30, end: 31 ),
                //        new( begin: 31, end: 35 ),
                //        new( begin: 35, end: 40 ),
                //        new( begin: 40, end: 41 ),
                //        new( begin: 41, end: 42 ),
                //        new( begin: 42, end: 43 ),
                //        new( begin: 43, end: 44 ),
                //        new( begin: 44, end: 45 ),
                //        null }
                //}
            };

        var output = originalStrings
            .Select(originalString =>
                tokenizer.Encode(
                    tokenizer, originalString, null, 128,
                    TruncationStrategy.LongestFirst, 0))
            .ToList();

        foreach (var (expected, actual) in expectedResults.Zip(output))
        {
            Assert.Equal(expected.TokenIds, actual.TokenIds);
            Assert.Equal(expected.TokenOffsets, actual.TokenOffsets);
        }
    }
}
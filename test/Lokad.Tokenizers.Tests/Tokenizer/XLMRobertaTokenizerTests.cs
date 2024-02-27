using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public class XLMRobertaTokenizerTests
{
    [Fact]
    public async Task Test1()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "…";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 153, 2 },
            SegmentIds = new List<byte> { 0, 0, 0 },
            SpecialTokensMask = new List<byte> { 1, 0, 1 },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> { null, new Offset(0, 1), null },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { Mask.Special, Mask.None, Mask.Special }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test2()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "This is a sample sentence to be tokénized";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 3293, 83, 10, 121413, 149357, 47, 186, 25636, 2746, 29367, 2 },
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
              new Offset( 0, 4 ),
              new Offset( 4, 7 ),
              new Offset( 7, 9 ),
              new Offset( 9, 16 ),
              new Offset( 16, 25),
              new Offset( 25, 28),
              new Offset( 28, 31),
              new Offset( 31, 35),
              new Offset( 35, 38),
              new Offset( 38, 42),
              null,
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test2_1()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "tokénized";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 25636, 2746, 29367, 2 },
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
              new Offset( 0, 3 ),
              new Offset( 3, 6 ),
              new Offset( 6, 10 ),
              null,
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test3()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "Wondering how this will get tokenized 🤔 ?";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 76648, 214, 3642, 903, 1221, 2046, 47, 1098, 29367, 6, 243691, 705, 2 },
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
                new Offset(0,  6 ),
                new Offset(6,  9 ),
                new Offset(9,  13 ),
                new Offset(13, 18 ),
                new Offset(18, 23 ),
                new Offset(23, 27 ),
                new Offset(27, 30 ),
                new Offset(30, 33 ),
                new Offset(33, 37 ),
                new Offset(37, 38 ),
                new Offset(38, 39 ),
                new Offset(39, 41 ),
              null,
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test3_1()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "hi 🤔";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 1274, 6, 243691, 2 },
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
                new Offset( 0, 2 ),
                new Offset( 2, 3 ),
                new Offset( 3, 4 ),
              null,
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test3_2()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = " 🤔 ?";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 6, 243691, 705, 2 },
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
                new Offset( 0, 1 ),
                new Offset( 1, 2 ),
                new Offset( 2, 4 ),
              null,
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test4()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "İs th!s 𩸽 Ϻ Šœ Ugljšić dấu nặng";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 63770, 5675, 38, 7, 6, 3, 6, 3, 3608, 52908, 345, 11016, 170, 36277, 39973, 55315, 2 },
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
                new Offset( 0, 2),
                new Offset( 2, 5),
                new Offset( 5, 6),
                new Offset( 6, 7),
                new Offset( 7, 8),
                new Offset( 8, 9),
                new Offset( 9, 10),
                new Offset( 10, 11),
                new Offset( 11, 13),
                new Offset( 13, 14),
                new Offset( 14, 16),
                new Offset( 16, 18),
                new Offset( 18, 19),
                new Offset( 19, 22),
                new Offset( 22, 26),
                new Offset( 26, 31),
              null,
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test4_1()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "İs th!s";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 63770, 5675, 38, 7, 2 },
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
                new Offset( 0, 2),
                new Offset( 2, 5),
                new Offset( 5, 6),
                new Offset( 6, 7),
              null,
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test5()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = "   İs th!s    𩸽 Ϻ Šœ   Ugljšić  dấu nặng     ";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 6, 6, 63770, 5675, 38, 7, 6, 6, 6, 6, 3, 6, 3, 3608, 52908, 6, 6, 345, 11016,
                   170, 36277, 6, 39973, 55315, 6, 6, 6, 6, 6, 2},
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
                new Offset( 0, 1 ),
                new Offset( 1, 2 ),
                new Offset( 2, 5 ),
                new Offset( 5, 8 ),
                new Offset( 8, 9 ),
                new Offset( 9, 10 ),
                new Offset( 10, 11 ),
                new Offset( 11, 12 ),
                new Offset( 12, 13 ),
                new Offset( 13, 14 ),
                new Offset( 14, 15 ),
                new Offset( 15, 16 ),
                new Offset( 16, 17 ),
                new Offset( 17, 19 ),
                new Offset( 19, 20 ),
                new Offset( 20, 21 ),
                new Offset( 21, 22 ),
                new Offset( 22, 24 ),
                new Offset( 24, 26 ),
                new Offset( 26, 27 ),
                new Offset( 27, 30 ),
                new Offset( 30, 31 ),
                new Offset( 31, 35 ),
                new Offset( 35, 40 ),
                new Offset( 40, 41 ),
                new Offset( 41, 42 ),
                new Offset( 42, 43 ),
                new Offset( 43, 44 ),
                new Offset( 44, 45 ),
              null
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }

    [Fact]
    public async Task Test6()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var original_string = " � İs th!s �� 𩸽 Ϻ Šœ   Ugljšić  dấu nặng     ";

        var expected_result = new TokenizedInput
        {
            TokenIds = new List<long> { 0, 6, 63770, 5675, 38, 7, 6, 6, 3, 6, 3, 3608, 52908, 6, 6, 345, 11016, 170, 36277,
                   6, 39973, 55315, 6, 6, 6, 6, 6, 2},
            SegmentIds = new List<byte> { },
            SpecialTokensMask = new List<byte> { },
            OverflowingTokens = new List<long>(),
            NumTruncatedTokens = 0,
            TokenOffsets = new List<Offset?> {
              null,
                new Offset ( 0,  1 ),
                new Offset ( 2,  5 ),
                new Offset ( 5,  8 ),
                new Offset ( 8,  9 ),
                new Offset (9,  10 ),
                new Offset (10, 11 ),
                new Offset (13, 14 ),
                new Offset (14, 15 ),
                new Offset (15, 16 ),
                new Offset (16, 17 ),
                new Offset (17, 19 ),
                new Offset (19, 20 ),
                new Offset (20, 21 ),
                new Offset (21, 22 ),
                new Offset (22, 24 ),
                new Offset (24, 26 ),
                new Offset (26, 27 ),
                new Offset (27, 30 ),
                new Offset (30, 31 ),
                new Offset (31, 35 ),
                new Offset (35, 40 ),
                new Offset (40, 41 ),
                new Offset (41, 42 ),
                new Offset (42, 43 ),
                new Offset (43, 44 ),
                new Offset (44, 45 ),
              null
            },
            ReferenceOffsets = new List<List<uint>>(),
            Mask = new List<Mask> { }
        };

        // When
        var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, original_string, null, 128, TruncationStrategy.LongestFirst, 0);

        // Then
        Assert.Equal(expected_result.TokenOffsets, result.TokenOffsets);
        Assert.Equal(expected_result.TokenIds, result.TokenIds);
    }
}
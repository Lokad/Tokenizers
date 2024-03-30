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

    [Fact]
    public async Task Test_E5_Small_Input_Texts_HuggingFace_Default_Example_Should_Pass()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var input_texts = new List<string> {
            "query: how much protein should a female eat",
            "query: 南瓜的家常做法",
            "passage: As a general guideline, the CDC's average requirement of protein for women ages 19 to 70 is 46 grams per day. But, as you can see from this chart, you'll need to increase that if you're expecting or training for a marathon. Check out the chart below to see how much protein you should be eating each day.",
            "passage: 1.清炒南瓜丝 原料:嫩南瓜半个 调料:葱、盐、白糖、鸡精 做法: 1、南瓜用刀薄薄的削去表面一层皮,用勺子刮去瓤 2、擦成细丝(没有擦菜板就用刀慢慢切成细丝) 3、锅烧热放油,入葱花煸出香味 4、入南瓜丝快速翻炒一分钟左右,放盐、一点白糖和鸡精调味出锅"
        };

        var expected_result = JsonConvert.DeserializeObject<List<List<long>>>(File.ReadAllText("./Fixture/e5-small-input-texts-tokenized-hf-default-example.json"));

        foreach (var (First, Second) in input_texts.Zip(expected_result))
        {
            var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, First, null, 128, TruncationStrategy.LongestFirst, 0);
            // Then
            Assert.Equal(Second.Count, result.TokenIds.Count);
            Assert.Equal(Second, result.TokenIds);
        }
    }

    [Fact]
    public async Task Test_E5_Small_Input_Texts_LLM_Example1_Should_Pass()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var input_texts = new List<string> {
            "query: Wie viel Protein sollte eine Frau essen?",
            "passage: Als allgemeine Richtlinie gilt, dass der durchschnittliche Proteinbedarf der CDC für Frauen im Alter von 19 bis 70 Jahren 46 Gramm pro Tag beträgt. Wie Sie dieser Tabelle entnehmen können, müssen Sie diesen Wert jedoch erhöhen, wenn Sie schwanger sind oder für einen Marathon trainieren. Sehen Sie sich die folgende Tabelle an, um zu sehen, wie viel Protein Sie täglich zu sich nehmen sollten.",
            "query: ¿Cuánta proteína debe comer una mujer?",
            "passage: Como pauta general, el requerimiento promedio de proteína de los CDC para mujeres de 19 a 70 años es de 46 gramos por día. Pero, como puede ver en esta tabla, deberá aumentarlo si está embarazada o entrenando para un maratón. Consulte la tabla a continuación para ver cuánta proteína debe consumir cada día.",
            "query: كم يجب أن تأكل المرأة من البروتين؟",
            "passage: كدليل عام، فإن متطلبات البروتين المتوسطة لمراكز السيطرة على الأمراض للنساء اللواتي تتراوح أعمارهن بين 19 و70 عامًا هي 46 جرامًا في اليوم. ولكن، كما ترى من هذا الرسم البياني، ستحتاجين إلى زيادة ذلك إذا كنتِ تتوقعين أو تتدربين من أجل سباق الماراثون. تحقق من الرسم البياني أدناه لمعرفة مقدار البروتين الذي يجب أن تتناولينه كل يوم."
        };

        var expected_result = JsonConvert.DeserializeObject<List<List<long>>>(File.ReadAllText("./Fixture/e5-small-input-texts-tokenized-llm-example1.json"));

        foreach (var (First, Second) in input_texts.Zip(expected_result))
        {
            var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, First, null, 128, TruncationStrategy.LongestFirst, 0);
            // Then
            Assert.Equal(Second.Count, result.TokenIds.Count);
            Assert.Equal(Second, result.TokenIds);
        }
    }

    [Fact]
    public async Task Test_E5_Small_Input_Texts_LLM_French_Examples_Should_Pass()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var input_texts = new List<string> {
            "query: Où est l'arrêt de bus le plus proche ?",
            "passage: L'arrêt de bus le plus proche se trouve à l'angle de la rue de Rivoli et de la rue Saint-Antoine.",
            "query: Quel est le meilleur restaurant de Paris ?",
            "passage: Le meilleur restaurant de Paris est le Jules Verne, situé au deuxième étage de la tour Eiffel.",
            "query: Quelle est la meilleure façon de visiter le Louvre ?",
            "passage: La meilleure façon de visiter le Louvre est de prendre un audioguide et de suivre un itinéraire thématique."      
        };

        var expected_result = JsonConvert.DeserializeObject<List<List<long>>>(File.ReadAllText("./Fixture/e5-small-input-texts-tokenized-llm-french-examples.json"));

        // When
        foreach (var (First, Second) in input_texts.Zip(expected_result))
        {
            var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, First, null, 128, TruncationStrategy.LongestFirst, 0);
            // Then
            Assert.Equal(Second.Count, result.TokenIds.Count);
            Assert.Equal(Second, result.TokenIds);
        }
    }

    [Fact]
    public async Task Test_E5_Small_Input_Texts_LLM_German_Examples_Should_Pass()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var input_texts = new List<string> {
            "query: Wo ist die nächste Bushaltestelle?",
            "passage: Die nächste Bushaltestelle befindet sich an der Ecke Rivolistraße und Saint-Antoine-Straße.",
            "query: Welches ist das beste Restaurant in Paris?",
            "passage: Das beste Restaurant in Paris ist das Jules Verne, das sich im zweiten Stock des Eiffelturms befindet.",
            "query: Wie kann man den Louvre am besten besichtigen?",
            "passage: Die beste Möglichkeit, den Louvre zu besichtigen, ist, einen Audioguide zu nehmen und einem thematischen Rundgang zu folgen."
        };

        var expected_result = JsonConvert.DeserializeObject<List<List<long>>>(File.ReadAllText("./Fixture/e5-small-input-texts-tokenized-llm-german-examples.json"));

        // When
        foreach (var (First, Second) in input_texts.Zip(expected_result))
        {
            var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, First, null, 128, TruncationStrategy.LongestFirst, 0);
            // Then
            Assert.Equal(Second.Count, result.TokenIds.Count);
            Assert.Equal(Second, result.TokenIds);
        }
    }

    [Fact]
    public async Task Test_E5_Small_Input_Texts_LLM_Spanish_Examples_Should_Pass()
    {
        // Given
        var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");
        var xlm_roberta_tokenizer = new XLMRobertaTokenizer(vocab_path, false);
        var input_texts = new List<string> {
            "query: ¿Dónde está la parada de autobús más cercana?",
            "passage: La parada de autobús más cercana se encuentra en la esquina de la calle de Rivoli y de la calle Saint-Antoine.",
            "query: ¿Cuál es el mejor restaurante de París?",
            "passage: El mejor restaurante de París es el Jules Verne, situado en el segundo piso de la torre Eiffel.",
            "query: ¿Cuál es la mejor manera de visitar el Louvre?",
            "passage: La mejor manera de visitar el Louvre es tomar una audioguía y seguir un itinerario temático."
        };

        var expected_result = JsonConvert.DeserializeObject<List<List<long>>>(File.ReadAllText("./Fixture/e5-small-input-texts-tokenized-llm-spanish-examples.json"));

        // When
        foreach (var (First, Second) in input_texts.Zip(expected_result))
        {
            var result = xlm_roberta_tokenizer.Encode(xlm_roberta_tokenizer, First, null, 128, TruncationStrategy.LongestFirst, 0);
            // Then
            Assert.Equal(Second.Count, result.TokenIds.Count);
            Assert.Equal(Second, result.TokenIds);
        }
    }

}
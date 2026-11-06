using System.Text;
using System.Text.Json;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

[Collection("XLM-R")]
public sealed class XLMRobertaTokenizerTests(XlmTokenizerFixture fixture)
{
    static string PathFor(string name) => Path.Combine(AppContext.BaseDirectory,"Fixture",name);
    public static IEnumerable<object[]> Cases(string section)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(PathFor("xlmr-cases.json")));
        foreach (var item in document.RootElement.GetProperty(section).EnumerateArray())
            yield return new object[] { item.GetProperty("name").GetString()!,item.GetRawText() };
    }

    [Theory]
    [MemberData(nameof(Cases),"singleCases")]
    public void LegacyIdsAndOffsetsRemainStable(string name,string data)
    {
        Assert.NotEmpty(name);
        using var document = JsonDocument.Parse(data);
        var expected = document.RootElement;
        string text = expected.GetProperty("text").GetString()!;
        int maximum = expected.GetProperty("maxLength").GetInt32();
        var result = fixture.Original.Encode(text,null,maximum,
            Enum.Parse<TruncationStrategy>(expected.GetProperty("strategy").GetString()!),0);
        Assert.Equal(expected.GetProperty("ids").EnumerateArray().Select(i => i.GetInt64()),result.TokenIds);
        Assert.Equal(expected.GetProperty("offsets").EnumerateArray().Select(o => o.ValueKind == JsonValueKind.Null
            ? null : new Offset(o[0].GetUInt32(),o[1].GetUInt32())),result.TokenOffsets);
        VerifyMetadata(result,text,maximum);
    }

    [Theory]
    [MemberData(nameof(Cases),"corpora")]
    public void EveryGoldenCorpusRowIsExercised(string name,string data)
    {
        Assert.NotEmpty(name);
        using var document = JsonDocument.Parse(data);
        var corpus = document.RootElement;
        string[] texts = corpus.GetProperty("inputs").EnumerateArray().Select(t => t.GetString()!).ToArray();
        var expected = JsonSerializer.Deserialize<long[][]>(File.ReadAllText(PathFor(corpus.GetProperty("expectedFile").GetString()!)))
            ?? throw new InvalidDataException("Expected an array of token-ID rows");
        Assert.NotEmpty(texts);
        Assert.Equal(texts.Length,expected.Length);
        for (int i = 0; i < texts.Length; i++)
        {
            var result = fixture.Original.Encode(texts[i],null,128,TruncationStrategy.LongestFirst,0);
            Assert.Equal(expected[i],result.TokenIds);
            VerifyMetadata(result,texts[i],128);
        }
    }

    static void VerifyMetadata(TokenizedInput result,string text,int maximum)
    {
        int count = result.TokenIds.Count;
        Assert.InRange(count,2,maximum);
        Assert.Equal(count,result.SegmentIds.Count);
        Assert.Equal(count,result.SpecialTokensMask.Count);
        Assert.Equal(count,result.TokenOffsets.Count);
        Assert.Equal(count,result.ReferenceOffsets.Count);
        Assert.Equal(count,result.Mask.Count);
        Assert.All(result.SegmentIds,id => Assert.Equal((byte)0,id));
        Assert.Equal(0,result.TokenIds[0]);
        Assert.Equal(2,result.TokenIds[^1]);
        int length = text.EnumerateRunes().Count();
        for (int i = 0; i < count; i++)
        {
            if (result.TokenOffsets[i] is { } offset)
            {
                Assert.InRange(offset.Begin,0u,(uint)length);
                Assert.InRange(offset.End,offset.Begin+1,(uint)length);
                Assert.NotEmpty(result.ReferenceOffsets[i]);
                Assert.All(result.ReferenceOffsets[i],p => Assert.InRange(p,offset.Begin,offset.End-1));
            }
            else
            {
                Assert.Equal(Mask.Special,result.Mask[i]);
                Assert.Equal((byte)1,result.SpecialTokensMask[i]);
                Assert.Empty(result.ReferenceOffsets[i]);
            }
        }
        Assert.True(result.OverflowingTokens.Count >= result.NumTruncatedTokens);
        if (result.NumTruncatedTokens == 0) Assert.Empty(result.OverflowingTokens);
    }

    [Theory]
    [InlineData(TruncationStrategy.LongestFirst)]
    [InlineData(TruncationStrategy.OnlyFirst)]
    [InlineData(TruncationStrategy.OnlySecond)]
    public void PairTemplatesAndTruncationRemainAligned(TruncationStrategy strategy)
    {
        var result = fixture.Original.Encode("hello world","a b c d e",11,strategy,1);
        Assert.Equal(11,result.TokenIds.Count);
        Assert.Equal(11,result.TokenOffsets.Count);
        Assert.Equal(11,result.ReferenceOffsets.Count);
        Assert.Equal(11,result.Mask.Count);
        Assert.Equal(11,result.SpecialTokensMask.Count);
        Assert.Equal(11,result.SegmentIds.Count);
        Assert.Equal(0,result.TokenIds[0]);
        Assert.Equal(2,result.TokenIds[^1]);
        Assert.Equal(4,result.SpecialTokensMask.Count(v => v == 1));
        Assert.Equal(result.SpecialTokensMask.Select(v => v == 1),result.TokenOffsets.Select(o => o == null));
        Assert.True(result.NumTruncatedTokens > 0);
        Assert.All(result.TokenOffsets.Select((offset,index) => (offset,index)).Where(p => p.offset == null),
            p => { Assert.Empty(result.ReferenceOffsets[p.index]); Assert.Equal(Mask.Special,result.Mask[p.index]); });
    }
}

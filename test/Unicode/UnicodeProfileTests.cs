using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Unicode;

public sealed class UnicodeProfileTests
{
    [Fact]
    public void UnchangedMaybeNormalizationDoesNotMaterializeSourceMaps()
    {
        string text = string.Concat(Enumerable.Repeat("q\u0301", 2000));
        Assert.False(UnicodeProfile.CanSkipNormalization(text, NormalizationForm.FormKC));
        var tracked = new Token(text);
        var light = new Token(text, false);
        var positions = tracked.ReferenceOffsets;
        var offset = tracked.Offset;
        TokenizationUtils.DecomposeNfkc(tracked); TokenizationUtils.DecomposeNfkc(light);
        long before = GC.GetAllocatedBytesForCurrentThread();
        TokenizationUtils.DecomposeNfkc(tracked);
        long trackedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        TokenizationUtils.DecomposeNfkc(light);
        long lightBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(trackedBytes <= lightBytes + 256, $"Tracked: {trackedBytes}, text: {lightBytes}");
        Assert.Null(tracked.ScalarReferences);
        Assert.Same(text, tracked.Text);
        Assert.Same(positions, tracked.ReferenceOffsets);
        Assert.Same(offset, tracked.Offset);
    }

    private static readonly Lazy<string[][]> Conformance = new(() =>
    {
        using var input = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Unicode/NormalizationTest-15.1.0.txt.gz"));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var data = new MemoryStream();
        gzip.CopyTo(data);
        byte[] corpus = data.ToArray();
        Assert.Equal("871238e37e3be0696ec2bd0891119a041b052da1a84485eda05a5438724b223e",Convert.ToHexStringLower(SHA256.HashData(corpus)));
        return Encoding.UTF8.GetString(corpus).Split('\n').Select(line => line.Split('#')[0].Trim())
            .Where(line => line.Length > 0 && line[0] != '@').Select(line => line.Split(';').Take(5)
                .Select(field => string.Concat(field.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(code => char.ConvertFromUtf32(int.Parse(code, NumberStyles.HexNumber, CultureInfo.InvariantCulture))))).ToArray()).ToArray();
    });

    [Theory]
    [InlineData(NormalizationForm.FormC)]
    [InlineData(NormalizationForm.FormD)]
    [InlineData(NormalizationForm.FormKC)]
    [InlineData(NormalizationForm.FormKD)]
    public void OfficialUnicode151NormalizationConformance(NormalizationForm form)
    {
        Assert.True(Conformance.Value.Length > 19000);
        int[] targets = form switch
        {
            NormalizationForm.FormC => [1,1,1,3,3], NormalizationForm.FormD => [2,2,2,4,4],
            NormalizationForm.FormKC => [3,3,3,3,3], _ => [4,4,4,4,4]
        };
        foreach (string[] row in Conformance.Value)
            for (int i = 0; i < 5; i++) Assert.Equal(row[targets[i]], UnicodeProfile.Normalize(row[i], form));
    }

    [Theory]
    [InlineData("㋿", "令和")]
    [InlineData("\U0001E030", "а")]
    [InlineData("\U0001CCF0", "\U0001CCF0")]
    [InlineData("\uFFFE\uFFFF\U0010FFFF", "\uFFFE\uFFFF\U0010FFFF")]
    public void VersionedNormalizationAndNoncharacterPolicy(string input, string expected) =>
        Assert.Equal(expected, UnicodeProfile.Normalize(input, NormalizationForm.FormKC));

    [Fact]
    public void InvalidUtf16IsRejectedEvenAfterADecomposableCharacter() =>
        Assert.Throws<ArgumentException>(() => UnicodeProfile.Normalize("ﬁ" + (char)0xD800, NormalizationForm.FormKC));

    [Fact]
    public void MarkingInputSpecialDoesNotBypassUtf16Validation()
    {
        var tokenizer=new BaseTokenizer<BaseVocab>(new BaseVocab(new(){["[UNK]"]=0},new()),false,false);
        var token=new Token(new string((char)0xD800,1)){Mask=Mask.Special};
        Assert.Throws<ArgumentException>(()=>tokenizer.TokenizeToTokens(token));
    }

    [Fact]
    public void LiteralRegistrationRejectsInvalidUtf16()
    {
        string invalid=new((char)0xD800,1);
        var vocab=new BaseVocab(new(){["[UNK]"]=0,[invalid]=1},new(){MaskToken=invalid});
        Assert.Throws<ArgumentException>(()=>new BaseTokenizer<BaseVocab>(vocab,false,false));
    }

    [Theory]
    [InlineData("\uA7CB","\uA7CB")]
    [InlineData("\u0130","i\u0307")]
    [InlineData("\u1F88","\u1F80")]
    [InlineData("\U00010400","\U00010428")]
    public void PublicScalarCasingUsesUnicode151(string input,string expected)
    {
        var tokenizer=new BaseTokenizer<BaseVocab>(new BaseVocab(new(){["[UNK]"]=0},new()),true,false);
        Assert.Equal(expected,Assert.Single(tokenizer.Tokenize(input)));
    }

    [Theory]
    [InlineData("abc🧪")]
    [InlineData("東京𩸽")]
    [InlineData("가각")]
    public void IdentityTransformsRetainTextAndCallerMapping(string text)
    {
        var token = new Token(text);
        var positions = token.ReferenceOffsets;
        var offset = token.Offset;
        TokenizationUtils.CleanText(token);
        TokenizationUtils.Lowercase(token);
        TokenizationUtils.StripAccents(token);
        TokenizationUtils.ReplaceWhitespace(token);
        Assert.Same(text, token.Text);
        Assert.Same(positions, token.ReferenceOffsets);
        Assert.Same(offset, token.Offset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangingCaseAvoidsPerScalarBoxing(bool supplementary)
    {
        var tokenizer = new BaseTokenizer<BaseVocab>(new BaseVocab(new() { ["[UNK]"]=0 }, new()), true, false);
        string input = supplementary ? string.Concat(Enumerable.Repeat("A🧪", 2000)) : new string('A', 4000);
        string expected = supplementary ? string.Concat(Enumerable.Repeat("a🧪", 2000)) : new string('a', 4000);
        tokenizer.Tokenize(input);
        long before = GC.GetAllocatedBytesForCurrentThread();
        var result = tokenizer.Tokenize(input);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(expected, Assert.Single(result));
        Assert.True(allocated < input.Length * 8 + 4096, $"Unexpected per-scalar allocation: {allocated}");
    }
}

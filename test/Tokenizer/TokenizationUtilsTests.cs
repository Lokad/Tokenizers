using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
namespace Lokad.Tokenizers.Tests.Tokenizer;
public class TokenizationUtilsTests
{

    [Fact]
    public void TestCharIndicesPos01()
    {
        var s = "…";
        var charPositions = new List<int>();

        TokenizationUtils.CharIndicesForRunes(s)
            .ToList()
            .ForEach(x => charPositions.Add(x.Index));

        charPositions.Add(Encoding.UTF8.GetByteCount(s));

        // Expected positions
        var expected = new List<int> { 0, 3 };

        // Assert that the character positions match the expected positions
        Assert.Equal(expected, charPositions);
    }

    [Fact]
    public void TestCharIndicesPos02()
    {
        var s = "tokénized";
        var charPositions = new List<int>();

        TokenizationUtils.CharIndicesForRunes(s)
            .ToList()
            .ForEach(x => charPositions.Add(x.Index));

        charPositions.Add(Encoding.UTF8.GetByteCount(s));

        // Expected positions
        var expected = new List<int> { 0, 1, 2, 3, 4, 6, 7, 8, 9, 10, 11 };

        // Assert that the character positions match the expected positions
        Assert.Equal(expected, charPositions);
    }

    [Fact]
    public void TestCharIndicesPos03()
    {
        var s = " 🤔 ?";
        var charPositions = new List<int>();

        TokenizationUtils.CharIndicesForRunes(s)
            .ToList()
            .ForEach(x => charPositions.Add(x.Index));

        charPositions.Add(Encoding.UTF8.GetByteCount(s));

        // Expected positions
        var expected = new List<int> { 0, 1, 5, 6, 7 };

        // Assert that the character positions match the expected positions
        Assert.Equal(expected, charPositions);
    }

    [Fact]
    public void TestCharIndicesPos04()
    {
        var s = "İs th!s 𩸽 Ϻ Šœ Ugljšić dấu nặng";
        var charPositions = new List<int>();

        TokenizationUtils.CharIndicesForRunes(s)
            .ToList()
            .ForEach(x => charPositions.Add(x.Index));

        charPositions.Add(Encoding.UTF8.GetByteCount(s));

        // Expected positions
        var expected = new List<int> { 0, 2, 3, 4, 5, 6, 7, 8, 9, 13, 14, 16, 17, 19, 21, 22, 23, 24, 25, 26, 28, 29, 31, 32, 33,
        36, 37, 38, 39, 42, 43, 44 };

        // Assert that the character positions match the expected positions
        Assert.Equal(expected, charPositions);
    }

    [Fact]
    public void TestCharIndicesPos05()
    {
        var s = "   İs th!s    𩸽 Ϻ Šœ   Ugljšić  dấu nặng     ";
        var charPositions = new List<int>();

        TokenizationUtils.CharIndicesForRunes(s)
            .ToList()
            .ForEach(x => charPositions.Add(x.Index));

        charPositions.Add(Encoding.UTF8.GetByteCount(s));

        // Expected positions
        var expected = new List<int> {0, 1, 2, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 19, 20, 22, 23, 25, 27, 28, 29, 30, 31,
        32, 33, 34, 36, 37, 39, 40, 41, 42, 45, 46, 47, 48, 51, 52, 53, 54, 55, 56, 57, 58,
         };

        // Assert that the character positions match the expected positions
        Assert.Equal(expected, charPositions);
    }

    [Fact]
    public void TestCharIndicesPos06()
    {
        var s = " � İs th!s �� 𩸽 Ϻ Šœ   Ugljšić  dấu nặng     ";
        var charPositions = new List<int>();

        TokenizationUtils.CharIndicesForRunes(s)
            .ToList()
            .ForEach(x => charPositions.Add(x.Index));

        charPositions.Add(Encoding.UTF8.GetByteCount(s));

        // Expected positions
        var expected = new List<int> {0, 1, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 21, 25, 26, 28, 29, 31, 33, 34, 35, 36,
        37, 38, 39, 40, 42, 43, 45, 46, 47, 48, 51, 52, 53, 54, 57, 58, 59, 60, 61, 62, 63, 64,};

        // Assert that the character positions match the expected positions
        Assert.Equal(expected, charPositions);
    }

    [Fact]
    public void TestSubString_01()
    {
        var s = Encoding.UTF8.GetBytes("▁tokénized");


        var prefix = TokenizationUtils.SubstringByByteOffset(s, 0);

        // Expected positions
        var expected = Encoding.UTF8.GetBytes("▁tokénized");
        // Assert that the character positions match the expected positions
        Assert.Equal(expected, prefix);
    }

    [Fact]
    public void TestSubString_02()
    {
        var s = Encoding.UTF8.GetBytes("▁tokénized");


        var prefix = TokenizationUtils.SubstringByByteOffset(s, 3);

        // Expected positions
        var expected = Encoding.UTF8.GetBytes("tokénized");
        // Assert that the character positions match the expected positions
        Assert.Equal(expected, prefix);
    }

    [Fact]
    public void TestCharsCount_01()
    {
        var s = "y̆";

        var count = new StringInfo(s).LengthInTextElements;
        var expected = 1;

        Assert.Equal(expected, count);
    }

    [Fact]
    public void TestCharsCount_02()
    {
        var s = "İs th!s 𩸽 Ϻ Šœ Ugljšić dấu nặng";

        var count = new StringInfo(s).LengthInTextElements;
        var expected = 31;

        Assert.Equal(expected, count);
    }

    [Fact]
    public void TestCharsCount_03()
    {
        var s = "Wondering how this will get tokenized 🤔 ?";

        var count = new StringInfo(s).LengthInTextElements;
        var expected = 41;

        Assert.Equal(expected, count);
    }

    [Fact]
    public void TestCharsCount_04()
    {
        var s = "🤔";

        var count = new StringInfo(s).LengthInTextElements;
        var expected = 1;

        Assert.Equal(expected, count);
    }


}

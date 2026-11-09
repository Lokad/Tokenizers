using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class LiteralMatchingTests
{
    [Theory]
    [InlineData(1000)]
    [InlineData(4000)]
    public void MatchingWorkIsBoundedIndependentlyOfCorrectOutputs(int length)
    {
        string prefix = new('a',length);
        var matcher = new LiteralTokenMatcher([(prefix+"b",7L)]);
        var miss = matcher.FindMatches(prefix,out long missWork);
        Assert.False(matcher.TryMatch(miss,0,out _));
        Assert.InRange(missWork,0,2L*prefix.Length);
        string exact = prefix+"b"+prefix;
        var hit = matcher.FindMatches(exact,out long hitWork);
        Assert.True(matcher.TryMatch(hit,0,out var token));
        Assert.Equal(7,token.Id);
        Assert.InRange(hitWork,0,2L*exact.Length);
        var supplementary = new LiteralTokenMatcher([("🧪a",8L)]);
        string unicode = string.Concat(Enumerable.Repeat("🧪a",length));
        var matches = supplementary.FindMatches(unicode,out long unicodeWork);
        Assert.True(supplementary.TryMatch(matches,unicode.Length-3,out _));
        Assert.InRange(unicodeWork,0,2L*unicode.Length);
    }

    [Fact]
    public void SparseLiteralOccurrencesAvoidInputSizedTemporaryArrays()
    {
        var matcher = new LiteralTokenMatcher([("<mask>",7L)]);
        string text = "<mask>"+new string('a',200_000);
        matcher.FindMatches(text);
        long before = GC.GetAllocatedBytesForCurrentThread();
        var matches = matcher.FindMatches(text);
        long allocated = GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.True(allocated < 16_384,$"Sparse matching allocated {allocated} bytes");
        Assert.True(matcher.TryMatch(matches,0,out var found));
        Assert.Equal(7,found.Id);
        Assert.False(matcher.TryMatch(matches,text.Length-1,out _));
    }

    [Fact]
    public void GreedyLiteralMatchesAgreeWithAnIndependentOrdinalSearch()
    {
        string[] literals = ["a", "aa", "aaaaab", "ab", "ba", "🧪", "🧪a", "a🧪", "two words", " "];
        var matcher = new LiteralTokenMatcher(literals.Select((text, id) => (text, (long)id)));
        var random = new Random(731);
        for (int sample = 0; sample < 100; sample++)
        {
            string text = string.Concat(Enumerable.Range(0, 50).Select(_ => literals[random.Next(literals.Length)]));
            var matches = matcher.FindMatches(text);
            for (int position = 0; position < text.Length;)
            {
                string? expected = literals.Where(p => text.AsSpan(position).StartsWith(p, StringComparison.Ordinal))
                    .OrderByDescending(p => p.Length).FirstOrDefault();
                bool found = matcher.TryMatch(matches, position, out var actual);
                Assert.Equal(expected != null, found);
                if (expected != null) { Assert.Equal(expected, actual.Text); Assert.Equal(Array.IndexOf(literals, expected), actual.Id); }
                position += expected?.Length ?? 1;
            }
        }
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(4000)]
    public void LongPrefixFailureAndSuccessHaveIndependentControls(int length)
    {
        string prefix = new('a', length);
        var matcher = new LiteralTokenMatcher([(prefix + "b", 7L)]);
        var failure = matcher.FindMatches(prefix);
        for (int i = 0; i < prefix.Length; i++) Assert.False(matcher.TryMatch(failure, i, out _));
        var success = matcher.FindMatches("x" + prefix + "b" + prefix);
        Assert.False(matcher.TryMatch(success, 0, out _));
        Assert.True(matcher.TryMatch(success, 1, out var match));
        Assert.Equal(7, match.Id);
        Assert.False(matcher.TryMatch(success, length + 2, out _));
    }
}

using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class LiteralMatchingTests
{
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

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Immutable literal matching. Reverse scanning finds the longest match at each start in linear transitions.</summary>
internal sealed class LiteralTokenMatcher
{
    internal readonly record struct Match(string Text, long Id);
    private sealed class Node
    {
        internal readonly Dictionary<int, Node> Children = new();
        internal Match? Token;
    }
    private readonly PrefixMatcher<Match> matcher;
    private readonly HashSet<char> startingCharacters = new();
    internal bool HasWhitespaceLiterals { get; }

    internal LiteralTokenMatcher(IEnumerable<(string Text, long Id)> tokens)
    {
        var root = new Node();
        foreach (var (text, id) in tokens)
        {
            ArgumentException.ThrowIfNullOrEmpty(text);
            UnicodeProfile.ValidateUtf16(text);
            if (UnicodeProfile.IsWhiteSpace(text)) HasWhitespaceLiterals = true;
            startingCharacters.Add(text[0]);
            var node = root;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (!node.Children.TryGetValue(text[i], out var child))
                    node.Children.Add(text[i], child = new());
                node = child;
            }
            if (node.Token != null) throw new ArgumentException("Duplicate literal token");
            node.Token = new Match(text, id);
        }
        matcher = PrefixMatcher<Match>.Create(root, n => n.Children.Select(p => (p.Key, p.Value)),
            n => (n.Token.HasValue, n.Token.GetValueOrDefault()));
    }

    // One integer per UTF-16 position avoids enumerating all overlapping occurrences.
    internal int[] FindMatches(string text)
    {
        if (!text.Any(startingCharacters.Contains)) return [];
        int[]? matches = null;
        int state = 0;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            state = matcher.Step(state, text[i]);
            int match = matcher.FirstMatch(state);
            if (match == 0) continue;
            matches ??= new int[text.Length];
            matches[i] = match;
        }
        return matches ?? [];
    }

    internal bool TryMatch(int[] matches, int position, out Match match)
    {
        int state = matches.Length == 0 ? 0 : matches[position];
        match = state == 0 ? default : matcher.Value(state);
        return state != 0;
    }

    internal bool ContainsWhitespaceLiteral(string text)
    {
        if (!HasWhitespaceLiterals) return false;
        return ContainsMatch(text);
    }

    internal bool ContainsMatch(string text)
    {
        int state = 0;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            state = matcher.Step(state, text[i]);
            if (matcher.FirstMatch(state) != 0) return true;
        }
        return false;
    }
}

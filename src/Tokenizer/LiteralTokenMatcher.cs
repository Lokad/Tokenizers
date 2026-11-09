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

    // Sparse occurrences do not require input-sized indexing. Dense matches
    // switch to an array once its size is bounded by the number of occurrences.
    internal sealed class MatchMap
    {
        internal static readonly MatchMap Empty = new(0);
        private int[]? dense;
        private Dictionary<int,int>? sparse;
        private readonly int length;
        internal MatchMap(int length)
        {
            this.length = length;
            if (length <= 256) dense = new int[length];
            else sparse = new();
        }
        internal void Add(int position, int state)
        {
            if (dense != null) { dense[position] = state; return; }
            sparse!.Add(position, state);
            if (sparse.Count < Math.Max(16, length / 16)) return;
            dense = new int[length];
            foreach (var entry in sparse) dense[entry.Key] = entry.Value;
            sparse = null;
        }
        internal int StateAt(int position) => dense is { Length: > 0 } ? dense[position] : sparse?.GetValueOrDefault(position) ?? 0;
    }

    internal MatchMap FindMatches(string text) => FindMatches(text, out _);

    internal MatchMap FindMatches(string text, out long transitionWork)
    {
        transitionWork = 0;
        if (!text.Any(startingCharacters.Contains)) return MatchMap.Empty;
        MatchMap? matches = null;
        int state = 0;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            state = matcher.Step(state, text[i], out int work);
            transitionWork += work;
            int match = matcher.FirstMatch(state);
            if (match == 0) continue;
            matches ??= new MatchMap(text.Length);
            matches.Add(i, match);
        }
        return matches ?? MatchMap.Empty;
    }

    internal bool TryMatch(MatchMap matches, int position, out Match match)
    {
        int state = matches.StateAt(position);
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

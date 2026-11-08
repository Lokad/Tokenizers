namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Immutable longest-prefix matching for literal added tokens.</summary>
internal sealed class LiteralTokenMatcher
{
    internal readonly record struct Match(string Text, long Id);
    private sealed class Node
    {
        internal readonly Dictionary<char, Node> Children = new();
        internal Match? Token;
    }
    private readonly Node root = new();
    internal bool HasWhitespaceLiterals { get; }

    internal LiteralTokenMatcher(IEnumerable<(string Text, long Id)> tokens)
    {
        foreach (var (text, id) in tokens)
        {
            ArgumentException.ThrowIfNullOrEmpty(text);
            UnicodeProfile.ValidateUtf16(text);
            if (UnicodeProfile.IsWhiteSpace(text)) HasWhitespaceLiterals = true;
            var node = root;
            foreach (char character in text)
            {
                if (!node.Children.TryGetValue(character, out var child))
                    node.Children.Add(character, child = new());
                node = child;
            }
            if (node.Token != null) throw new ArgumentException("Duplicate literal token");
            node.Token = new Match(text, id);
        }
    }

    internal bool TryMatch(ReadOnlySpan<char> text, out Match match)
    {
        var node = root;
        Match? selected = null;
        foreach (char character in text)
        {
            if (!node.Children.TryGetValue(character, out node)) break;
            if (node.Token is { } token) selected = token;
        }
        match = selected.GetValueOrDefault();
        return selected.HasValue;
    }

    internal bool ContainsWhitespaceLiteral(string text)
    {
        if (!HasWhitespaceLiterals) return false;
        for (int i = 0; i < text.Length; i++)
            if (TryMatch(text.AsSpan(i), out _)) return true;
        return false;
    }
}

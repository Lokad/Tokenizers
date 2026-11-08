using System.Text;

namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Represents a node in a trie structure used for tokenization.
/// </summary>
public class TrieNode
{
    private string text;
    private int prefixLength;
    private Dictionary<Rune, TrieNode>? children;
    /// <summary>
    /// Gets or sets the text associated with this node.
    /// </summary>
    public string Text
    {
        get
        {
            if (prefixLength != text.Length) text = text[..prefixLength];
            return text;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            text = value;
            prefixLength = value.Length;
        }
    }

    /// <summary>
    /// Gets or sets the length of the text in runes.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the score of the node.
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Gets or sets the index of the node.
    /// </summary>
    public long Index { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node is the end of a word.
    /// </summary>
    public bool End { get; set; }

    /// <summary>
    /// Gets or sets the children of this node.
    /// </summary>
    public Dictionary<Rune, TrieNode> Children
    {
        get => children ??= new();
        set => children = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal TrieNode? FindChild(Rune rune) => children?.GetValueOrDefault(rune);

    internal TrieNode GetOrAddChild(Rune rune, string piece, int prefixEnd, int scalarLength)
    {
        var child = FindChild(rune);
        if (child != null) return child;
        child = new TrieNode(piece, prefixEnd, scalarLength);
        Children.Add(rune, child);
        return child;
    }

    // Prefixes share a complete piece until a public caller requests their text.
    // Terminal text is set to the vocabulary's original string by the loader.
    internal TrieNode(string text, int prefixLength, int scalarLength)
    {
        this.text = text;
        this.prefixLength = prefixLength;
        Length = scalarLength;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrieNode"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text associated with this node.</param>
    public TrieNode(string text) : this(text ?? throw new ArgumentNullException(nameof(text)), text.Length, text.EnumerateRunes().Count()) { }
}

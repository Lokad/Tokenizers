using System.Text;

namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Represents a node in a trie structure used for tokenization.
/// </summary>
public class TrieNode
{
    /// <summary>
    /// Gets or sets the text associated with this node.
    /// </summary>
    public string Text { get; set; }

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
    public Dictionary<Rune, TrieNode> Children { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrieNode"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text associated with this node.</param>
    public TrieNode(string text)
    {
        Text = text;
        Length = text.EnumerateRunes().Count();
        Children = new Dictionary<Rune, TrieNode>();
    }
}

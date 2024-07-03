using System.Text;

namespace Lokad.Tokenizers.Vocab;

public class TrieNode
{
    public string Text { get; set; }
    public int Length { get; set; }
    public float Score { get; set; }
    public long Index { get; set; }
    public bool End { get; set; }
    public Dictionary<Rune, TrieNode> Children { get; set; }

    public TrieNode(string text)
    {
        Text = text;
        Length = text.EnumerateRunes().Count();
        Children = new Dictionary<Rune, TrieNode>();
    }
}

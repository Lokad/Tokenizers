// Port notes:
// - OffsetSize is ported as 'uint'
// - Token and TokenRef have been merged as 'Token'.

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Owned token that references the original text but stores its own string representation.
/// </summary>
public class Token : ITokenTrait
{
    /// <summary>
    /// String representation
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Start and end positions of the token with respect to the original text
    /// </summary>
    public Offset Offset { get; set; }

    /// <summary>
    /// Sequence of positions with respect to the original text contained in the token.
    /// For example, if the token offset is `start: 4, end: 10`, corresponding reference_offsets are `[4, 5, 6, 7, 8, 9]`
    /// </summary>
    public IReadOnlyList<uint> ReferenceOffsets { get; set; }

    /// <summary>
    /// Mask indicating the type of the token
    /// </summary>
    public Mask Mask { get; set; }

    /// <summary>
    /// Creates a new owned token from a `String`.
    /// </summary>
    /// <param name="text">text reference</param>
    public Token(string text)
    {
        Text = text;
        var text_size = (uint)text.Length;
        Offset = new Offset(0, text_size);
        ReferenceOffsets = Enumerable.Range(0, (int)text_size).Select(i => (uint)i).ToList();
        Mask = Mask.None;
    }

    /// <summary>
    /// Creates a new token from a text and list of offsets.
    /// </summary>
    /// <param name="text">text reference</param>
    /// <param name="offsets">reference positions with respect to the original text</param>
    public Token(string text, uint[] offsets)
    {
        Text = text;
        Offset = new Offset(0, (uint)offsets.Length);
        ReferenceOffsets = offsets;
        Mask = Mask.None;
    }

    public Token(string text, Offset offset, IReadOnlyList<uint> referenceOffsets, Mask mask)
    {
        Text = text;
        Offset = offset;
        ReferenceOffsets = referenceOffsets;
        Mask = mask;
    }

    public string AsStr()
    {
        return Text;
    }

    public static Token From(string text)
    {
        return new Token(text);
    }

    public Token Clone()
    {
        return new Token(Text, Offset, new List<uint>(ReferenceOffsets), Mask);
    }
}

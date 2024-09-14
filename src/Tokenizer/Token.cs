using System.Text;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// token that references the original text but stores its own string representation.
/// </summary>
public class Token : IToken
{

    public byte[] Bytes { get; private set; }

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


    public Token(byte[] bytes)
    {
        Bytes = bytes;
        Text = Encoding.UTF8.GetString(bytes);
        var text_size = (uint)Text.Length;
        Offset = new Offset(0, text_size);
        ReferenceOffsets = Enumerable.Range(0, (int)text_size).Select(i => (uint)i).ToList();
        Mask = Mask.None;
    }

    public Token(byte[] bytes, uint[] offsets)
    {
        Bytes = bytes;
        Text = Encoding.UTF8.GetString(bytes);
        var text_size = (uint)Text.Length;
        Offset = new Offset(0, (uint)offsets.Length);
        ReferenceOffsets = offsets;
        Mask = Mask.None;
    }

    public Token(byte[] bytes, uint[] offsets, Mask mask)
    {
        Bytes = bytes;
        Text = Encoding.UTF8.GetString(bytes);
        var text_size = (uint)Text.Length;
        Offset = new Offset(0, (uint)offsets.Length);
        ReferenceOffsets = offsets;
        Mask = mask;
    }

    public Token(byte[] bytes, Offset offset, IReadOnlyList<uint> referenceOffsets, Mask mask)
    {
        Bytes = bytes;
        Text = Encoding.UTF8.GetString(bytes);
        var text_size = (uint)Text.Length;
        Offset = offset;
        ReferenceOffsets = referenceOffsets;
        Mask = mask;
    }


    public override string ToString()
    {
        return Text;
    }

    public Token Clone()
    {
        return new Token(Bytes, Offset, new List<uint>(ReferenceOffsets), Mask);
    }
}

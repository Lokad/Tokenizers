using System.Text;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>A token with positions measured in Unicode scalars in the original input.</summary>
public class Token : IToken
{
    public string Text { get; set; }
    public Offset Offset { get; set; }
    private IReadOnlyList<uint> referenceOffsets = [];
    /// <summary>One starting source position for each scalar in Text.</summary>
    public IReadOnlyList<uint> ReferenceOffsets
    {
        get => referenceOffsets;
        set { referenceOffsets = value ?? throw new ArgumentNullException(nameof(value)); ScalarReferences = null; }
    }
    internal uint[][]? ScalarReferences { get; set; }
    public Mask Mask { get; set; }

    public Token(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        int count = text.EnumerateRunes().Count();
        Offset = new Offset(0, (uint)count);
        ReferenceOffsets = Enumerable.Range(0, count).Select(i => (uint)i).ToArray();
    }

    public Token(string text, uint[] offsets) : this(text)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        ReferenceOffsets = offsets.ToArray();
        Offset = offsets.Length == 0 ? new Offset(0, 0) : new Offset(offsets.Min(), checked(offsets.Max() + 1));
    }

    public Token(string text, Offset offset, IReadOnlyList<uint> referenceOffsets, Mask mask)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        ArgumentNullException.ThrowIfNull(offset);
        Offset = new Offset(offset.Begin, offset.End);
        ReferenceOffsets = referenceOffsets.ToArray();
        Mask = mask;
    }

    public override string ToString() => Text;
    public static Token From(string text) => new(text);
    public Token Clone() => new(Text, Offset, ReferenceOffsets, Mask)
    {
        ScalarReferences = ScalarReferences?.Select(r => r.ToArray()).ToArray()
    };
}

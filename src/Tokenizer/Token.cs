using System.Text;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>A token with positions measured in Unicode scalars in the original input.</summary>
public class Token : IToken
{
    private string text;
    private int? scalarCount;
    private Offset? offset;
    private IReadOnlyList<uint>? referenceOffsets;
    public string Text
    {
        get => text;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            // Public text edits retain the original default offsets, as eager construction did.
            if (TracksOffsets && text != value)
            {
                _ = ReferenceOffsets;
                _ = Offset;
            }
            ReplaceText(value);
        }
    }
    public Offset Offset
    {
        get => offset ??= new Offset(0, (uint)ScalarCount);
        set => offset = value ?? throw new ArgumentNullException(nameof(value));
    }
    internal int ScalarCount => scalarCount ??= text.EnumerateRunes().Count();
    internal bool TracksOffsets { get; } = true;
    internal void ReplaceText(string value) { text = value; scalarCount = null; }
    /// <summary>One starting source position for each scalar in Text.</summary>
    public IReadOnlyList<uint> ReferenceOffsets
    {
        get => referenceOffsets ??= Enumerable.Range(0, ScalarCount).Select(i => (uint)i).ToArray();
        set { referenceOffsets = value ?? throw new ArgumentNullException(nameof(value)); ScalarReferences = null; }
    }
    internal uint[][]? ScalarReferences { get; set; }
    public Mask Mask { get; set; }

    public Token(string text) : this(text, true) { }

    internal Token(string text, bool trackOffsets)
    {
        ArgumentNullException.ThrowIfNull(text);
        this.text = text;
        TracksOffsets = trackOffsets;
        if (!trackOffsets) referenceOffsets = [];
    }

    public Token(string text, uint[] offsets) : this(text)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        ReferenceOffsets = offsets.ToArray();
        Offset = offsets.Length == 0 ? new Offset(0, 0) : new Offset(offsets.Min(), checked(offsets.Max() + 1));
    }

    public Token(string text, Offset offset, IReadOnlyList<uint> referenceOffsets, Mask mask)
    {
        this.text = text ?? throw new ArgumentNullException(nameof(text));
        ArgumentNullException.ThrowIfNull(offset);
        Offset = new Offset(offset.Begin, offset.End);
        ReferenceOffsets = referenceOffsets.ToArray();
        Mask = mask;
    }

    public override string ToString() => Text;
    public static Token From(string text) => new(text);
    public Token Clone() => !TracksOffsets ? new(Text, false) { Mask = Mask } : new(Text, Offset, ReferenceOffsets, Mask)
    {
        ScalarReferences = ScalarReferences?.Select(r => r.ToArray()).ToArray()
    };
}

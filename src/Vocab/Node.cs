namespace Lokad.Tokenizers.Vocab;

using System.Text;
using Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Represents a node in the vocabulary with text, score, index, and reference offsets.
/// </summary>
public class Node
{
    private readonly uint[][] references;
    private IReadOnlyList<IReadOnlyList<uint>>? publicReferences;
    internal uint[][] References => references;

    /// <summary>Read-only contributing source positions for each scalar in Text. This is the authoritative mapping.</summary>
    public IReadOnlyList<IReadOnlyList<uint>> ScalarReferences => publicReferences ??=
        Array.AsReadOnly(references.Select(r => (IReadOnlyList<uint>)Array.AsReadOnly(r)).ToArray());

    /// <summary>Construct an owned node with a complete, potentially many-to-one source mapping.</summary>
    public static Node FromScalarReferences(string text, float score, long index, int start, int end,
        IEnumerable<IEnumerable<uint>> scalarReferences)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(scalarReferences);
        UnicodeProfile.ValidateUtf16(text);
        var mapped = scalarReferences.Select(r => r?.ToArray() ?? throw new ArgumentException("Source groups must not be null", nameof(scalarReferences))).ToArray();
        if (mapped.Length != text.EnumerateRunes().Count() || mapped.Any(r => r.Length == 0))
            throw new ArgumentException("Supply one nonempty source group per scalar", nameof(scalarReferences));
        return Create(text,score,index,start,end,mapped);
    }

    internal static Node Create(string text, float score, long index, int start, int end, uint[][] references) =>
        new(text,score,index,start,end,references);

    private Node(string text, float score, long index, int start, int end, uint[][] references)
    {
        Text=text; Score=score; Index=index; Start=start; End=end;
        this.references=references;
    }

    private static uint[][] LegacyReferences(string text, uint[] positions)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(positions);
        UnicodeProfile.ValidateUtf16(text);
        int count=text.EnumerateRunes().Count();
        if (count == positions.Length) return positions.Select(p=>new[] { p }).ToArray();
        if (count == 1 && positions.Length > 0) return [positions.ToArray()];
        throw new ArgumentException("Ambiguous source coverage: use FromScalarReferences for many-to-one or expanding mappings", nameof(positions));
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    /// <param name="text">The text of the node.</param>
    /// <param name="score">The score of the node.</param>
    /// <param name="index">The index of the node.</param>
    /// <param name="start">The start position of the node.</param>
    /// <param name="end">The end position of the node.</param>
    /// <param name="referenceOffsets">One source position per scalar, or all contributors for a single scalar. The array is copied. Use FromScalarReferences for richer mappings.</param>
    public Node(string text, float score, long index, int start, int end, uint[] referenceOffsets)
        : this(text,score,index,start,end,LegacyReferences(text,referenceOffsets)) { }

    /// <summary>
    /// Gets the text of the node.
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// Gets the score of the node.
    /// </summary>
    public float Score { get; private set; }

    /// <summary>
    /// Gets the index of the node.
    /// </summary>
    public long Index { get; private set; }

    /// <summary>
    /// Gets the start position of the node.
    /// </summary>
    public int Start { get; private set; }

    /// <summary>
    /// Gets the end position of the node.
    /// </summary>
    public int End { get; private set; }

    /// <summary>
    /// Gets a sorted, unique copy of the node's aggregate source coverage. Editing it does not mutate the node.
    /// </summary>
    public uint[] ReferenceOffsets => TokenAlignment.SourcePositions(references);
}

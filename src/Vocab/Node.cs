namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Represents a node in the vocabulary with text, score, index, and reference offsets.
/// </summary>
public class Node
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    /// <param name="text">The text of the node.</param>
    /// <param name="score">The score of the node.</param>
    /// <param name="index">The index of the node.</param>
    /// <param name="start">The start position of the node.</param>
    /// <param name="end">The end position of the node.</param>
    /// <param name="referenceOffsets">The reference offsets of the node.</param>
    public Node(string text, float score, long index, int start, int end, uint[] referenceOffsets)
    {
        Text = text;
        Score = score;
        Index = index;
        Start = start;
        End = end;
        ReferenceOffsets = referenceOffsets;
    }

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
    /// Gets the reference offsets of the node.
    /// </summary>
    public uint[] ReferenceOffsets { get; private set; }
}

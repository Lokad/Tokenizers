namespace Lokad.Tokenizers.Vocab;

public class Node
{
    public Node(string text, float score, long index, int start, int end, uint[] referenceOffsets)
    {
        Text = text;
        Score = score;
        Index = index;
        Start = start;
        End = end;
        ReferenceOffsets = referenceOffsets;
    }

    public string Text { get; private set; }
    public float Score { get; private set; }
    public long Index { get; private set; }
    public int Start { get; private set; }
    public int End { get; private set; }
    public uint[] ReferenceOffsets { get; private set; }
}

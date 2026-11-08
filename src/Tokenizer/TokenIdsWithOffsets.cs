namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Encoded sequence
/// Intermediate tokenization steps before addition of special tokens, after encoding
/// </summary>
public class TokenIdsWithOffsets
{
    internal void Validate()
    {
        if (Ids == null || Offsets == null || ReferenceOffsets == null || Masks == null ||
            Ids.Count != Offsets.Count || Ids.Count != ReferenceOffsets.Count || Ids.Count != Masks.Count ||
            ReferenceOffsets.Any(r => r == null))
            throw new ArgumentException("IDs, offsets, reference offsets and masks must have equal lengths");
    }

    /// <summary>
    /// Vector of token IDs
    /// </summary>
    public List<long> Ids { get; set; } = [];

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are represented by null offsets or empty reference lists.
    /// </summary>
    public List<Offset?> Offsets { get; set; } = [];

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are represented by null offsets or empty reference lists.
    /// </summary>
    public List<List<uint>> ReferenceOffsets { get; set; } = [];

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This list has the same length as Ids.
    /// </summary>
    public List<Mask> Masks { get; set; } = [];
}

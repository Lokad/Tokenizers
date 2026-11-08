namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Encoded input with special tokens
/// Combines already-truncated content with the model's special-token template.
/// </summary>
public class TokenIdsWithSpecialTokens
{
    internal void Append(TokenIdsWithOffsets input, byte segment, bool takeOwnership = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Validate();
        TokenIds.AddRange(input.Ids);
        SegmentIds.AddRange(Enumerable.Repeat(segment, input.Ids.Count));
        SpecialTokensMask.AddRange(Enumerable.Repeat((byte)0, input.Ids.Count));
        TokenOffsets.AddRange(takeOwnership ? input.Offsets : input.Offsets.Select(o => o == null ? null : new Offset(o.Begin, o.End)));
        ReferenceOffsets.AddRange(takeOwnership ? input.ReferenceOffsets : input.ReferenceOffsets.Select(r => r.ToList()));
        Mask.AddRange(input.Masks);
    }

    /// <summary>
    /// Vector of token IDs
    /// </summary>
    public List<long> TokenIds { get; set; } = [];

    /// <summary>
    /// Vector segments ids (for example for BERT segments are separated with a [SEP] marker, each incrementing the segment ID).
    /// This list has the same length as TokenIds.
    /// </summary>
    public List<byte> SegmentIds { get; set; } = [];

    /// <summary>
    /// Flags automatically inserted model-template tokens (1) or sequence tokens (0). This list has the same length as TokenIds.
    /// </summary>
    public List<byte> SpecialTokensMask { get; set; } = [];

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are represented by null offsets or empty reference lists.
    /// </summary>
    public List<Offset?> TokenOffsets { get; set; } = [];

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are represented by null offsets or empty reference lists.
    /// </summary>
    public List<List<uint>> ReferenceOffsets { get; set; } = [];

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This list has the same length as TokenIds.
    /// </summary>
    public List<Mask> Mask { get; set; } = [];
}

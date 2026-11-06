namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Encoded input with special tokens
/// Intermediate tokenization steps before truncation to a maximum length, after encoding and addition of special tokens
/// </summary>
public class TokenIdsWithSpecialTokens
{
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

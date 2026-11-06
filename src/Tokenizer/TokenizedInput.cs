namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Tokenized Input, ready for processing in language models
/// This represents the final output of the encoding process (tokenized sentence with encoded values)
/// </summary>
public class TokenizedInput
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
    /// Vector containing overflowing tokens, populated following a truncation step
    /// </summary>
    public List<long> OverflowingTokens { get; set; } = [];

    /// <summary>
    /// Number of removed tokens, excluding retained stride context in OverflowingTokens.
    /// </summary>
    public int NumTruncatedTokens { get; set; }

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

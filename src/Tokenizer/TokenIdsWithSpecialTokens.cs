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
    public List<long> TokenIds { get; set; }

    /// <summary>
    /// Vector segments ids (for example for BERT segments are separated with a [SEP] marker, each incrementing the segment ID).
    /// This vector has the same length as token_ids.
    /// </summary>
    public List<byte> SegmentIds { get; set; }

    /// <summary>
    /// Flags tokens as special tokens (1) or not (0). This vector has the same length as token_ids.
    /// </summary>
    public List<byte> SpecialTokensMask { get; set; }

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<Offset?> TokenOffsets { get; set; }

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<List<uint>> ReferenceOffsets { get; set; }

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This vector has the same length as token_ids.
    /// </summary>
    public List<Mask> Mask { get; set; }
}

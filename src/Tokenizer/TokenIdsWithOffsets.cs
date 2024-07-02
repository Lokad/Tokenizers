// Port notes:
// - OffsetSize is ported as 'uint'
// - Token and TokenRef have been merged as 'Token'.

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Encoded sequence
/// Intermediate tokenization steps before addition of special tokens, after encoding
/// </summary>
public class TokenIdsWithOffsets
{
    /// <summary>
    /// Vector of token IDs
    /// </summary>
    public List<long> Ids { get; set; }

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<Offset?> Offsets { get; set; }

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<List<uint>> ReferenceOffsets { get; set; }

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This vector has the same length as token_ids.
    /// </summary>
    public List<Mask> Masks { get; set; }
}

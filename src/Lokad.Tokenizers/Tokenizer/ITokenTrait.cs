// Port notes:
// - OffsetSize is ported as 'uint'
// - Token and TokenRef have been merged as 'Token'.

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Token abstraction trait to access token fields, irrespective of their form (reference of owned)
/// </summary>
public interface ITokenTrait
{
    /// <summary>
    /// Returns the offset of the token with respect to the original string
    /// </summary>
    Offset Offset { get; }

    /// <summary>
    /// Returns the token mask
    /// </summary>
    Mask Mask { get; }

    /// <summary>
    /// Returns a string representation for the token
    /// </summary>
    string AsStr();
}

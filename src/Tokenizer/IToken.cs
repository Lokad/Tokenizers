namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Token abstractions that can be used to represent a token in a tokenized string
/// </summary>
public interface IToken
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
    string ToString();
}

namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Interface representing a vocabulary for tokenization.
/// </summary>
public interface IVocab
{
    /// <summary>
    /// Gets the dictionary of token values and their corresponding IDs.
    /// </summary>
    Dictionary<string, long> Values { get; }

    /// <summary>
    /// Gets the dictionary of token IDs and their corresponding values.
    /// </summary>
    Dictionary<long, string> Indices { get; }

    /// <summary>
    /// Gets the dictionary of special token values and their corresponding IDs.
    /// </summary>
    Dictionary<string, long> SpecialValues { get; }

    /// <summary>
    /// Gets the dictionary of special token IDs and their corresponding values.
    /// </summary>
    Dictionary<long, string> SpecialIndices { get; }

    /// <summary>
    /// Gets the collection of special tokens.
    /// </summary>
    /// <returns>An enumerable collection of special tokens.</returns>
    IEnumerable<string> SpecialTokens();

    /// <summary>
    /// Gets the value representing an unknown token.
    /// </summary>
    /// <returns>The unknown token value.</returns>
    string GetUnknownValue();

    /// <summary>
    /// Gets the value representing the beginning of a sequence.
    /// </summary>
    /// <returns>The beginning of sequence token value.</returns>
    string GetBosValue();

    /// <summary>
    /// Gets the value representing a classification token.
    /// </summary>
    /// <returns>The classification token value.</returns>
    string GetClsValue();

    /// <summary>
    /// Gets the value representing the end of a sequence.
    /// </summary>
    /// <returns>The end of sequence token value.</returns>
    string GetEosValue();

    /// <summary>
    /// Gets the value representing a mask token.
    /// </summary>
    /// <returns>The mask token value.</returns>
    string GetMaskValue();

    /// <summary>
    /// Gets the value representing a padding token.
    /// </summary>
    /// <returns>The padding token value.</returns>
    string GetPadValue();

    /// <summary>
    /// Gets the value representing a separator token.
    /// </summary>
    /// <returns>The separator token value.</returns>
    string GetSepValue();

    /// <summary>
    /// Converts a token to its corresponding ID.
    /// </summary>
    /// <param name="token">The token to convert.</param>
    /// <returns>The ID of the token.</returns>
    long TokenToId(string token);

    /// <summary>
    /// Converts an ID to its corresponding token.
    /// </summary>
    /// <param name="id">The ID to convert.</param>
    /// <returns>The token corresponding to the ID.</returns>
    string IdToToken(long id);

    /// <summary>
    /// Converts a collection of tokens to their corresponding IDs.
    /// </summary>
    /// <param name="tokens">The tokens to convert.</param>
    /// <returns>A list of IDs corresponding to the tokens.</returns>
    List<long> ConvertTokensToIds(IEnumerable<string> tokens);

    /// <summary>
    /// Adds extra IDs to the vocabulary.
    /// </summary>
    /// <param name="numExtraIds">The number of extra IDs to add.</param>
    void AddExtraIds(long numExtraIds);

    /// <summary>
    /// Adds a collection of tokens to the vocabulary.
    /// </summary>
    /// <param name="tokens">The tokens to add.</param>
    void AddTokens(IEnumerable<string> tokens);
}

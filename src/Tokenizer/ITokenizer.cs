using Lokad.Tokenizers.Vocab;


namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Base trait for tokenizers
/// </summary>
public interface ITokenizer<T> where T : IVocab
{
    /// <summary>
    /// Returns a reference to the tokenizer vocabulary
    /// </summary>
    T Vocab { get; }

    /// <summary>
    /// Tokenize a string, returns a vector of tokens as strings.
    /// Use `TokenizeWithOffsets` or `TokenizeToTokens` to return offset information.
    /// </summary>
    /// <param name="text">text (string-like) to tokenize</param>
    /// <returns>`List<string>` containing the tokens string representation</returns>
    List<string> Tokenize(string text);

    /// <summary>
    /// Tokenize a string, returning tokens with offset information
    /// </summary>
    /// <param name="text">text (string-like) to tokenize</param>
    /// <returns>`TokensWithOffsets` with the tokens and their offset information</returns>
    TokensWithOffsets TokenizeWithOffsets(string text);

    /// <summary>
    /// Tokenize a TokenRef, returning a sequence of tokens
    /// </summary>
    /// <param name="initialToken">TokenRef to tokenize (this is especially useful for nested tokenization, where a tokenizer is called on the ouput of a pre-tokenizer, such as BERT).</param>
    /// <returns>`List<Token>` tokenization of the original `TokenRef`</returns>
    List<Token> TokenizeToTokens(Token initialToken);

    /// <summary>
    /// Convert a slice of string-like to a vector ot token indices
    /// </summary>
    /// <param name="tokens">list of token string-like to convert to ids</param>
    /// <returns>`List<long>` with the token indices</returns>
    List<long> ConvertTokensToIds(List<string> tokens);

    /// <summary>
    /// Converts a sequence of ids (integer) into a string, using the tokenizer and vocabulary
    /// with options to remove special tokens and clean up tokenization spaces.
    /// </summary>
    /// <param name="tokenIds">list of tokenized input ids. Can be obtained using the `Encode` or `EncodePlus` methods.</param>
    /// <param name="skipSpecialTokens">if set to True, will replace special tokens.</param>
    /// <param name="cleanUpTokenizationSpaces">if set to True, will clean up the tokenization spaces.</param>
    /// <returns>`string`: decoded sentence</returns>
    string Decode(List<long> tokenIds, bool skipSpecialTokens, bool cleanUpTokenizationSpaces);

    /// <summary>
    /// Converts a sequence of strings into a single string. This will clean-up artifacts from tokenization
    /// (for example `sub ##word`) and generate a single output string
    /// </summary>
    /// <param name="tokens">list of tokens to concatenate.</param>
    /// <returns>`string`: concatenated sentence string</returns>
    string ConvertTokensToString(List<string> tokens);

    /// <summary>
    /// Cleans-up tokenization artifacts (for example whitespace before punctuation)
    /// </summary>
    /// <param name="inputString">input string to clean up</param>
    /// <returns>`string`: clean-up string</returns>
    string CleanUpTokenization(string inputString);
}

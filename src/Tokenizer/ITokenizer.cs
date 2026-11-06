using Lokad.Tokenizers.Vocab;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Public encoding, decoding and source-offset contract for vocabulary-based tokenizers.</summary>
public interface ITokenizer<T> where T : IVocab
{
    T Vocab { get; }
    List<string> Tokenize(string text);
    TokensWithOffsets TokenizeWithOffsets(string text);
    List<Token> TokenizeToTokens(Token initialToken);
    List<long> ConvertTokensToIds(List<string> tokens);
    TokenizedInput Encode(string inputText, string? additionalInputText, int maxLen, TruncationStrategy truncationStrategy, int stride);
    string Decode(List<long> tokenIds, bool skipSpecialTokens, bool cleanUpTokenizationSpaces);
    string ConvertTokensToString(List<string> tokens);
    string CleanUpTokenization(string inputString);
}

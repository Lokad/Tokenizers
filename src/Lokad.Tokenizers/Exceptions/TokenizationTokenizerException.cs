namespace Lokad.Tokenizers.Exceptions;

public class TokenizationTokenizerException : TokenizerException
{
    public TokenizationTokenizerException(string message)
        : base($"Tokenization error: {message}") { }
}

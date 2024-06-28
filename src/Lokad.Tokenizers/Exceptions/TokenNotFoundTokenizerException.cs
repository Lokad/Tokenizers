namespace Lokad.Tokenizers.Exceptions;

public class TokenNotFoundTokenizerException : TokenizerException
{
    public TokenNotFoundTokenizerException(string message)
        : base($"Token not found in vocabulary: {message}") { }
}

namespace Lokad.Tokenizers.Exceptions;

public class ValueTokenizerException : TokenizerException
{
    public ValueTokenizerException(string message)
        : base($"Value error: {message}") { }
}

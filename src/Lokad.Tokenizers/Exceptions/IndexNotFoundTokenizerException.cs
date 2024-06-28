namespace Lokad.Tokenizers.Exceptions;

public class IndexNotFoundTokenizerException : TokenizerException
{
    public IndexNotFoundTokenizerException(string message)
        : base($"Token index not found in vocabulary: {message}") { }
}

namespace Lokad.Tokenizers.Exceptions;

public class FileNotFoundTokenizerException : TokenizerException
{
    public FileNotFoundTokenizerException(string message)
        : base($"File not found error: {message}") { }
}

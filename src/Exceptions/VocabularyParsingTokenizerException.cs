namespace Lokad.Tokenizers.Exceptions;

public class VocabularyParsingTokenizerException : TokenizerException
{
    public VocabularyParsingTokenizerException(string message)
        : base($"Error when loading vocabulary file, the file may be corrupted or does not match the expected format: {message}") { }
}

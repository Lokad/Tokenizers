namespace Lokad.Tokenizers.Exceptions;

[Obsolete("Retained for API compatibility; current tokenizers use standard data/argument exceptions or the active tokenizer exceptions.")]
public class VocabularyParsingTokenizerException : TokenizerException
{
    public VocabularyParsingTokenizerException(string message)
        : base($"Error when loading vocabulary file, the file may be corrupted or does not match the expected format: {message}") { }
}

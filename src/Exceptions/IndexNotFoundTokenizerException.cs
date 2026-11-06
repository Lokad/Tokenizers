namespace Lokad.Tokenizers.Exceptions;

[Obsolete("Retained for API compatibility; current tokenizers use standard data/argument exceptions or the active tokenizer exceptions.")]
public class IndexNotFoundTokenizerException : TokenizerException
{
    public IndexNotFoundTokenizerException(string message)
        : base($"Token index not found in vocabulary: {message}") { }
}

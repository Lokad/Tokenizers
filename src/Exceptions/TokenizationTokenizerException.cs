namespace Lokad.Tokenizers.Exceptions;

[Obsolete("Retained for API compatibility; current tokenizers use standard data/argument exceptions or the active tokenizer exceptions.")]
public class TokenizationTokenizerException : TokenizerException
{
    public TokenizationTokenizerException(string message)
        : base($"Tokenization error: {message}") { }
}

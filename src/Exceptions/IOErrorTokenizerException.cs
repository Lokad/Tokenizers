namespace Lokad.Tokenizers.Exceptions;

[Obsolete("Retained for API compatibility; current tokenizers use standard data/argument exceptions or the active tokenizer exceptions.")]
public class IOErrorTokenizerException : TokenizerException
{
    public IOErrorTokenizerException(string message)
        : base($"IO error: {message}") { }
}

using System;

namespace Lokad.Tokenizers.Exceptions;

public class IOErrorTokenizerException : TokenizerException
{
    public IOErrorTokenizerException(string message)
        : base($"IO error: {message}") { }
}

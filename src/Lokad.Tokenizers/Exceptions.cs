using System;

namespace Lokad.Tokenizers;

public class TokenizerException : Exception
{
    public TokenizerException(string message) : base(message) { }
}

public class FileNotFoundTokenizerException : TokenizerException
{
    public FileNotFoundTokenizerException(string message)
        : base($"File not found error: {message}") { }
}

public class VocabularyParsingTokenizerException : TokenizerException
{
    public VocabularyParsingTokenizerException(string message)
        : base($"Error when loading vocabulary file, the file may be corrupted or does not match the expected format: {message}") { }
}

public class IndexNotFoundTokenizerException : TokenizerException
{
    public IndexNotFoundTokenizerException(string message)
        : base($"Token index not found in vocabulary: {message}") { }
}

public class TokenNotFoundTokenizerException : TokenizerException
{
    public TokenNotFoundTokenizerException(string message)
        : base($"Token not found in vocabulary: {message}") { }
}

public class TokenizationTokenizerException : TokenizerException
{
    public TokenizationTokenizerException(string message)
        : base($"Tokenization error: {message}") { }
}

public class ValueTokenizerException : TokenizerException
{
    public ValueTokenizerException(string message)
        : base($"Value error: {message}") { }
}

public class IOErrorTokenizerException : TokenizerException
{
    public IOErrorTokenizerException(string message)
        : base($"IO error: {message}") { }
}

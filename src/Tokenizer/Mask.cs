namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Type indication for tokens (e.g. special token, white space, unknown...)
/// </summary>
public enum Mask
{
    /// <summary>
    /// The token has no particular mask. This is the default situation. It may indicate that further processing can be done on a token.
    /// </summary>
    None,

    /// <summary>
    /// The token represents a whitespace (in any shape or form)
    /// </summary>
    Whitespace,

    /// <summary>
    /// The token represents punctuation (in any shape or form)
    /// </summary>
    Punctuation,

    /// <summary>
    /// The token represents a single Chinese/Japanese/Korean character (including kana and hangul)
    /// </summary>
    CJK,

    /// <summary>
    /// The token is a special marker (such as a separator marker, a class marker, etc)
    /// </summary>
    Special,

    /// <summary>
    /// Compatibility state for a subtoken beginning. Built-in tokenizers use None for word beginnings and do not emit Begin.
    /// </summary>
    Begin,

    /// <summary>
    /// A continuation piece in unigram tokenization. Word beginnings use None; punctuation, whitespace, special and unknown pieces have their own masks.
    /// </summary>
    Continuation,

    /// <summary>
    /// Compatibility state for an unfinished subtoken. Built-in tokenizers do not emit this state.
    /// </summary>
    Unfinished,

    /// <summary>
    /// The token is out of vocabulary, it is unknown by the tokenizer and it will decode to unknown. Tokens that can be decoded properly (but may still be out of vocabulary) should not set this.
    /// </summary>
    Unknown,
}

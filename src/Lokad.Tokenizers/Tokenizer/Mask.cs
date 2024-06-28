// Port notes:
// - OffsetSize is ported as 'uint'
// - Token and TokenRef have been merged as 'Token'.

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
    /// The token is the begin in a series of subtokens, the offset refers specifically to the sub-token. Subsequent tokens in this sequence will carry the 'Continuation' mask
    /// </summary>
    Begin,

    /// <summary>
    /// The token is the continuation of the previous token, the offset refers specifically to the sub-token. All but the first sub-token in a sequence carry this mask (the first carries 'Begin'). (this is the reverse of Mask::Unfinished)
    /// </summary>
    Continuation,

    /// <summary>
    /// The token is the start of a token but not finished yet. All but the last sub-token in the a token sequence carry this mask. This is the reverse of Mask::Continuation.
    /// </summary>
    Unfinished,

    /// <summary>
    /// The token is out of vocabulary, it is unknown by the tokenizer and it will decode to unknown. Tokens that can be decoded properly (but may still be out of vocabulary) should not set this.
    /// </summary>
    Unknown,
}

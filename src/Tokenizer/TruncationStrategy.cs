// Port notes:
// - OffsetSize is ported as 'uint'
// - Token and TokenRef have been merged as 'Token'.

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Truncation strategy variants
/// Indicates if and how sequence pairs exceeding a given length should be truncated
/// </summary>
public enum TruncationStrategy
{
    /// <summary>
    /// Truncate the longest sequence first
    /// </summary>
    LongestFirst,

    /// <summary>
    /// Truncate only the first sequence
    /// </summary>
    OnlyFirst,

    /// <summary>
    /// Truncate only the second sequence
    /// </summary>
    OnlySecond,

    /// <summary>
    /// Do not truncate the sequences
    /// </summary>
    DoNotTruncate,
}

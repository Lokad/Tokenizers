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
    /// <remarks>Overflow retains the reverse removal order. Stride uses the trimmed sequence
    /// when only one side loses tokens, or the first sequence when both sides lose tokens.</remarks>
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

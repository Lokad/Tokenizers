using System.Text;
using Lokad.Tokenizers.Exceptions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("Lokad.Tokenizers.Tests")]
[assembly: InternalsVisibleToAttribute("GemmaConformance")]

namespace Lokad.Tokenizers.Tokenizer;

internal static class TokenizationUtils
{
    internal const char Metaspace = '\u2581';

    public static void CleanText(Token token) => Transform(token, Operation.Clean);

    public static bool IsWhitespace(Rune character) => character.Value is
        0x20 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x202F or 0x205F or 0x3000 or 9 or 10 or 13;

    private static bool IsControl(Rune character)
    {
        int value = character.Value;
        if (value is '\t' or '\r' or '\n') return false;
        return UnicodeProfile.IsControl(character);
    }

    public static void Lowercase(Token token) => Transform(token, Operation.Lowercase);
    internal static void ReplaceWhitespace(Token token) => Transform(token, Operation.Metaspace);

    public static void DecomposeNfkc(Token token) => TokenAlignment.Normalize(token, NormalizationForm.FormKC);

    public static void StripAccents(Token token)
    {
        // Canonical decomposition without removable marks would just be composed
        // again. Avoid that round trip (notably for already-normalized Hangul).
        bool hasMarks = false;
        foreach (Rune rune in token.Text.EnumerateRunes())
            if (UnicodeProfile.HasCanonicalNonSpacingMark(rune)) { hasMarks = true; break; }
        if (hasMarks)
        {
            TokenAlignment.Normalize(token, NormalizationForm.FormD);
            Transform(token, Operation.StripMarks);
        }
        TokenAlignment.Normalize(token, NormalizationForm.FormC);
    }

    private enum Operation { Clean, Lowercase, StripMarks, Metaspace }

    // Keep the input and its maps untouched until the first actual change.
    private static void Transform(Token token, Operation operation)
    {
        StringBuilder? text = null;
        uint[][]? references = null;
        List<uint[]>? mapped = null;
        int index = 0, utf16 = 0;
        foreach (Rune scalar in token.Text.EnumerateRunes())
        {
            bool remove = operation == Operation.Clean && (IsControl(scalar) || scalar.Value is 0 or 0xFFFD) ||
                operation == Operation.StripMarks && UnicodeProfile.IsNonSpacingMark(scalar);
            Rune replacement = operation == Operation.Clean && IsWhitespace(scalar) ? new Rune(' ') :
                operation == Operation.Metaspace && IsWhitespace(scalar) ? new Rune(Metaspace) : scalar;
            string? expansion = operation == Operation.Lowercase ? UnicodeProfile.Lowercase(scalar) : null;
            if (text == null && (remove || replacement != scalar || expansion != null))
            {
                text = new StringBuilder(token.Text.Length);
                text.Append(token.Text.AsSpan(0, utf16));
                if (token.TracksOffsets)
                {
                    references = TokenAlignment.References(token);
                    mapped = new List<uint[]>(references.Length);
                    for (int i = 0; i < index; i++) mapped.Add(references[i]);
                }
            }
            if (text != null && !remove)
            {
                if (expansion != null)
                {
                    text.Append(expansion);
                    if (mapped != null)
                        foreach (Rune _ in expansion.EnumerateRunes()) mapped.Add(references![index]);
                }
                else
                {
                    text.AppendRune(replacement);
                    mapped?.Add(references![index]);
                }
            }
            utf16 += scalar.Utf16SequenceLength;
            index++;
        }
        if (text == null) return;
        if (mapped == null) token.ReplaceText(text.ToString());
        else TokenAlignment.Set(token, text.ToString(), mapped.ToArray());
    }

    /// <summary>Truncate aligned sequences in place. Overflow includes optional retained stride context.</summary>
    public static (TokenIdsWithOffsets, TokenIdsWithOffsets?, List<long>, List<Offset?>) TruncateSequences(
        TokenIdsWithOffsets first, TokenIdsWithOffsets? second, int numTokensToRemove,
        TruncationStrategy strategy, int stride, bool includeOverflowOffsets = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numTokensToRemove);
        ArgumentOutOfRangeException.ThrowIfNegative(stride);
        if (!Enum.IsDefined(strategy)) throw new ArgumentOutOfRangeException(nameof(strategy));
        Validate(first);
        if (second != null) Validate(second);
        if (numTokensToRemove == 0) return (first, second, [], []);
        if (strategy == TruncationStrategy.DoNotTruncate)
            throw new ValueTokenizerException("Truncation needed but no truncation requested");
        if (strategy == TruncationStrategy.OnlySecond && second == null)
            throw new ValueTokenizerException("OnlySecond requires a second sequence");
        long available = strategy switch
        {
            TruncationStrategy.OnlyFirst => first.Ids.Count,
            TruncationStrategy.OnlySecond => second!.Ids.Count,
            _ => first.Ids.Count + (long)(second?.Ids.Count ?? 0)
        };
        if (numTokensToRemove > available) throw new ValueTokenizerException("Sequence too short for requested truncation");

        int removedFirst = 0, removedSecond = 0;
        var overflow = new List<long>(numTokensToRemove);
        var offsets = new List<Offset?>(includeOverflowOffsets ? numTokensToRemove : 0);
        // Record removals in reverse order, preserving the original paired LongestFirst contract.
        for (int i = 0; i < numTokensToRemove; i++)
        {
            bool useFirst = strategy == TruncationStrategy.OnlyFirst ||
                strategy == TruncationStrategy.LongestFirst &&
                (second == null || first.Ids.Count - removedFirst >= second.Ids.Count - removedSecond);
            var selected = useFirst ? first : second!;
            int index = selected.Ids.Count - 1 - (useFirst ? removedFirst++ : removedSecond++);
            overflow.Add(selected.Ids[index]);
            if (includeOverflowOffsets) offsets.Add(selected.Offsets[index]);
        }
        overflow.Reverse();
        offsets.Reverse();
        RemoveSuffix(first, removedFirst);
        if (second != null) RemoveSuffix(second, removedSecond);
        // A one-sided removal owns its stride context. Mixed LongestFirst removals
        // retain the legacy first-sequence context and reverse removal ordering.
        TokenIdsWithOffsets context = removedFirst == 0 ? second! : first;
        int length = Math.Min(stride, context.Ids.Count);
        if (length > 0)
        {
            overflow.InsertRange(0, context.Ids.GetRange(context.Ids.Count - length, length));
            if (includeOverflowOffsets) offsets.InsertRange(0, context.Offsets.GetRange(context.Offsets.Count - length, length));
        }
        return (first, second, overflow, offsets);
    }

    private static void Validate(TokenIdsWithOffsets tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        tokens.Validate();
    }

    private static void RemoveSuffix(TokenIdsWithOffsets tokens, int count)
    {
        int start = tokens.Ids.Count - count;
        tokens.Ids.RemoveRange(start, count);
        tokens.Offsets.RemoveRange(start, count);
        tokens.ReferenceOffsets.RemoveRange(start, count);
        tokens.Masks.RemoveRange(start, count);
    }
}

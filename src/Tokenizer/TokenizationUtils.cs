using System.Text;
using System.Globalization;
using Lokad.Tokenizers.Exceptions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("Lokad.Tokenizers.Tests")]

namespace Lokad.Tokenizers.Tokenizer;

internal static class TokenizationUtils
{
    internal const char Metaspace = '\u2581';

    /// <summary>
    /// Cleans text by removing control characters and normalizing whitespace
    /// </summary>
    public static void CleanText(Token token, bool strict)
    {
        var text = new StringBuilder(token.Text.Length);
        var references = TokenAlignment.References(token);
        var mapped = new List<uint[]>();
        int index = 0;
        foreach (Rune scalar in token.Text.EnumerateRunes())
        {
            if (!IsControl(scalar, strict) && scalar.Value is not 0 and not 0xFFFD)
            {
                text.Append(IsWhitespace(scalar) ? new Rune(' ') : scalar);
                mapped.Add(references[index]);
            }
            index++;
        }
        TokenAlignment.Set(token, text.ToString(), mapped.ToArray());
    }

    public static bool IsWhitespace(Rune character) => character.Value is
        0x20 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x202F or 0x205F or 0x3000 or 9 or 10 or 13;

    public static bool IsControl(Rune character, bool strict)
    {
        int value = character.Value;
        if (value is '\t' or '\r' or '\n') return false;
        if (!strict) return Rune.IsControl(character);
        return Rune.GetUnicodeCategory(character) is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.PrivateUse;
    }

    public static void Lowercase(Token token)
    {
        var text = new StringBuilder(token.Text.Length);
        uint[][] references = TokenAlignment.References(token);
        var mapped = new List<uint[]>();
        int index = 0;
        foreach (Rune scalar in token.Text.EnumerateRunes())
        {
            // Unicode default casing has one unconditional multi-scalar lowercase mapping.
            if (scalar.Value == 0x130)
            {
                text.Append("i\u0307");
                mapped.Add(references[index]);
                mapped.Add(references[index]);
            }
            else
            {
                text.Append(Rune.ToLowerInvariant(scalar));
                mapped.Add(references[index]);
            }
            index++;
        }
        TokenAlignment.Set(token, text.ToString(), mapped.ToArray());
    }

    public static void DecomposeNfkc(Token token) => TokenAlignment.Normalize(token, NormalizationForm.FormKC);

    public static void StripAccents(Token token)
    {
        TokenAlignment.Normalize(token, NormalizationForm.FormD);
        uint[][] references = TokenAlignment.References(token);
        var text = new StringBuilder();
        var mapped = new List<uint[]>();
        int index = 0;
        foreach (Rune scalar in token.Text.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(scalar) != UnicodeCategory.NonSpacingMark)
            {
                text.Append(scalar);
                mapped.Add(references[index]);
            }
            index++;
        }
        TokenAlignment.Set(token, text.ToString(), mapped.ToArray());
        TokenAlignment.Normalize(token, NormalizationForm.FormC);
    }

    /// <summary>Truncate aligned sequences in place. Overflow includes optional retained stride context.</summary>
    public static (TokenIdsWithOffsets, TokenIdsWithOffsets?, List<long>, List<Offset?>) TruncateSequences(
        TokenIdsWithOffsets first, TokenIdsWithOffsets? second, int numTokensToRemove,
        TruncationStrategy strategy, int stride)
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
        var offsets = new List<Offset?>(numTokensToRemove);
        // Record removals in reverse order, preserving the original paired LongestFirst contract.
        for (int i = 0; i < numTokensToRemove; i++)
        {
            bool useFirst = strategy == TruncationStrategy.OnlyFirst ||
                strategy == TruncationStrategy.LongestFirst &&
                (second == null || first.Ids.Count - removedFirst >= second.Ids.Count - removedSecond);
            var selected = useFirst ? first : second!;
            int index = selected.Ids.Count - 1 - (useFirst ? removedFirst++ : removedSecond++);
            overflow.Add(selected.Ids[index]);
            offsets.Add(selected.Offsets[index]);
        }
        overflow.Reverse();
        offsets.Reverse();
        RemoveSuffix(first, removedFirst);
        if (second != null) RemoveSuffix(second, removedSecond);
        TokenIdsWithOffsets context = strategy == TruncationStrategy.OnlySecond ? second! : first;
        int length = Math.Min(stride, context.Ids.Count);
        if (length > 0)
        {
            overflow.InsertRange(0, context.Ids.GetRange(context.Ids.Count - length, length));
            offsets.InsertRange(0, context.Offsets.GetRange(context.Offsets.Count - length, length));
        }
        return (first, second, overflow, offsets);
    }

    private static void Validate(TokenIdsWithOffsets tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (tokens.Ids.Count != tokens.Offsets.Count || tokens.Ids.Count != tokens.ReferenceOffsets.Count ||
            tokens.Ids.Count != tokens.Masks.Count)
            throw new ArgumentException("IDs, offsets, reference offsets and masks must have equal lengths", nameof(tokens));
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

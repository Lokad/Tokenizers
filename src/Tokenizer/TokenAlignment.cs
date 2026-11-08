using System.Text;
using System.Runtime.InteropServices;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Internal per-scalar source mappings, including many-to-one normalization.</summary>
internal static class TokenAlignment
{
    internal static uint[][] References(Token token)
    {
        int count = token.ScalarCount;
        if (token.ScalarReferences is { } mapped && mapped.Length == count) return mapped;
        if (token.ReferenceOffsets.Count != count)
            throw new ArgumentException("A token must supply one source position per Unicode scalar", nameof(token));
        return token.ReferenceOffsets.Select(p => new[] { p }).ToArray();
    }

    internal static void Set(Token token, string text, uint[][] references)
    {
        if (text.EnumerateRunes().Count() != references.Length || references.Any(r => r.Length == 0))
            throw new ArgumentException("Text and scalar source mappings must align", nameof(references));
        token.ReplaceText(text);
        var starts = new uint[references.Length];
        uint minimum = uint.MaxValue, maximum = 0;
        for (int i = 0; i < references.Length; i++)
        {
            uint start = uint.MaxValue;
            foreach (uint position in references[i])
            {
                start = Math.Min(start, position);
                minimum = Math.Min(minimum, position);
                maximum = Math.Max(maximum, position);
            }
            starts[i] = start;
        }
        token.ReferenceOffsets = starts;
        token.ScalarReferences = references;
        token.Offset = references.Length == 0 ? new Offset(0, 0) : new Offset(minimum, checked(maximum + 1));
    }

    internal static uint[] SourcePositions(Token token) =>
        token.ScalarReferences is { } mapped
            ? SourcePositions(mapped) : token.ReferenceOffsets.ToArray();

    internal static uint[] SourcePositions(uint[][] references)
    {
        int count = 0;
        foreach (var group in references) count = checked(count + group.Length);
        var positions = new uint[count];
        int unique = CopySourcePositions(references, positions);
        if (unique != positions.Length) Array.Resize(ref positions, unique);
        return positions;
    }

    internal static List<uint> SourcePositionList(Token token)
    {
        if (token.ScalarReferences is not { } mapped) return token.ReferenceOffsets.ToList();
        int count = 0;
        foreach (var group in mapped) count = checked(count + group.Length);
        var positions = new List<uint>(count);
        CollectionsMarshal.SetCount(positions, count);
        int unique = CopySourcePositions(mapped, CollectionsMarshal.AsSpan(positions));
        positions.RemoveRange(unique, count - unique);
        return positions;
    }

    private static int CopySourcePositions(uint[][] references, Span<uint> positions)
    {
        int index = 0;
        bool ordered = true;
        foreach (var group in references)
            foreach (uint position in group)
            {
                if (index > 0 && position < positions[index - 1]) ordered = false;
                positions[index++] = position;
            }
        if (!ordered) positions.Sort();
        int unique = 0;
        foreach (uint position in positions)
            if (unique == 0 || position != positions[unique - 1]) positions[unique++] = position;
        return unique;
    }

    // Callers compute the parent mapping once, outside their slicing loops.
    internal static Token Slice(string text, uint[][]? references, int scalarStart, int scalarCount, Mask mask)
    {
        var result = new Token(text, references != null) { Mask = mask };
        if (references != null) Set(result, text, references.AsSpan(scalarStart, scalarCount).ToArray());
        return result;
    }

    internal static void Normalize(Token token, NormalizationForm form)
    {
        if (UnicodeProfile.CanSkipNormalization(token.Text, form)) return;
        string normalized = UnicodeProfile.Normalize(token.Text, form,
            token.TracksOffsets ? () => References(token) : null, out var mapped);
        if (normalized == token.Text) return;
        if (mapped == null) token.ReplaceText(normalized);
        else Set(token, normalized, mapped);
    }
}

using System.Text;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Internal per-scalar source mappings, including many-to-one normalization.</summary>
internal static class TokenAlignment
{
    internal static uint[][] References(Token token)
    {
        int count = token.Text.EnumerateRunes().Count();
        if (token.ScalarReferences is { } mapped && mapped.Length == count) return mapped;
        if (token.ReferenceOffsets.Count != count)
            throw new ArgumentException("A token must supply one source position per Unicode scalar", nameof(token));
        return token.ReferenceOffsets.Select(p => new[] { p }).ToArray();
    }

    internal static void Set(Token token, string text, uint[][] references)
    {
        if (text.EnumerateRunes().Count() != references.Length || references.Any(r => r.Length == 0))
            throw new ArgumentException("Text and scalar source mappings must align", nameof(references));
        token.Text = text;
        token.ReferenceOffsets = references.Select(r => r.Min()).ToArray();
        token.ScalarReferences = references;
        uint[] positions = references.SelectMany(r => r).ToArray();
        token.Offset = positions.Length == 0 ? new Offset(0, 0) : new Offset(positions.Min(), checked(positions.Max() + 1));
    }

    internal static IReadOnlyList<uint> SourcePositions(Token token) =>
        token.ScalarReferences is { } mapped
            ? mapped.SelectMany(r => r).Distinct().Order().ToArray()
            : token.ReferenceOffsets;

    internal static Token Slice(Token token, string text, int scalarStart, int scalarCount, Mask mask)
    {
        var result = new Token(text) { Mask = mask };
        Set(result, text, References(token).AsSpan(scalarStart, scalarCount).ToArray());
        return result;
    }

    internal static void Normalize(Token token, NormalizationForm form)
    {
        string normalized = token.Text.Normalize(form);
        if (normalized == token.Text) return;
        var decomposition = form is NormalizationForm.FormKC or NormalizationForm.FormKD
            ? NormalizationForm.FormKD : NormalizationForm.FormD;
        var sources = new Dictionary<Rune, Queue<uint[]>>();
        uint[][] references = References(token);
        int index = 0;
        // Canonical reordering is stable for occurrences of the same scalar.
        // Queues preserve that order while composition combines their origins.
        foreach (Rune scalar in token.Text.EnumerateRunes())
        {
            foreach (Rune part in scalar.ToString().Normalize(decomposition).EnumerateRunes())
            {
                if (!sources.TryGetValue(part, out var queue)) sources.Add(part, queue = new());
                queue.Enqueue(references[index]);
            }
            index++;
        }
        var mapped = new List<uint[]>();
        foreach (Rune scalar in normalized.EnumerateRunes())
        {
            var positions = new SortedSet<uint>();
            foreach (Rune part in scalar.ToString().Normalize(NormalizationForm.FormD).EnumerateRunes())
                positions.UnionWith(sources[part].Dequeue());
            mapped.Add(positions.ToArray());
        }
        Set(token, normalized, mapped.ToArray());
    }
}

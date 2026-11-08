using System.Buffers;
using System.Text;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Unicode 15.1 data and UAX #15 normalization, independent of host ICU/NLS.</summary>
internal static class UnicodeProfile
{
    internal const string Version = "15.1.0";
    private readonly record struct Range(int First, int Last, byte Value);
    private readonly record struct Decomposition(bool Compatibility, int[] Scalars);
    private readonly record struct Unit(int Scalar, byte Combining, int Order, int Origin);
    private static readonly IComparer<Unit> canonicalOrder = Comparer<Unit>.Create(static (a, b) =>
        a.Combining != b.Combining ? a.Combining.CompareTo(b.Combining) : a.Order.CompareTo(b.Order));
    private static readonly Range[] properties, combining, quick;
    private static readonly byte[] ascii = new byte[128];
    private static readonly Dictionary<int, string> lower = new();
    private static readonly Dictionary<int, Decomposition> decompositions = new();
    private static readonly Dictionary<long, int> compositions = new();

    static UnicodeProfile()
    {
        using var stream = typeof(UnicodeProfile).Assembly.GetManifestResourceStream("Lokad.Tokenizers.Unicode15_1.bin")
            ?? throw new InvalidDataException("Missing Unicode profile");
        using var reader = new BinaryReader(stream);
        if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != "LTUCD151") throw new InvalidDataException("Invalid Unicode profile");
        Range[] ReadRanges()
        {
            var result = new Range[reader.ReadInt32()];
            for (int i = 0; i < result.Length; i++) result[i] = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadByte());
            return result;
        }
        int[] ReadScalars()
        {
            var result = new int[reader.ReadByte()];
            for (int i = 0; i < result.Length; i++) result[i] = reader.ReadInt32();
            return result;
        }
        properties = ReadRanges(); combining = ReadRanges(); quick = ReadRanges();
        for (int i = 0; i < ascii.Length; i++) ascii[i] = Lookup(properties, i);
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int code = reader.ReadInt32();
            var text = new StringBuilder();
            foreach (int value in ReadScalars()) text.AppendRune(new Rune(value));
            lower.Add(code, text.ToString());
        }
        count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int code = reader.ReadInt32();
            bool compatibility = reader.ReadBoolean();
            decompositions.Add(code, new(compatibility, ReadScalars()));
        }
        count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int left = reader.ReadInt32(), right = reader.ReadInt32();
            compositions.Add(Key(left, right), reader.ReadInt32());
        }
    }

    private static byte Lookup(Range[] ranges, int scalar)
    {
        int left = 0, right = ranges.Length - 1;
        while (left <= right)
        {
            int middle = left + (right - left) / 2;
            var range = ranges[middle];
            if (scalar < range.First) right = middle - 1;
            else if (scalar > range.Last) left = middle + 1;
            else return range.Value;
        }
        return 0;
    }

    internal static byte Properties(Rune rune) => rune.IsAscii ? ascii[rune.Value] : Lookup(properties, rune.Value);
    internal static bool IsControl(Rune rune) => (Properties(rune) & 1) != 0;
    internal static bool IsPunctuation(Rune rune) => (Properties(rune) & 2) != 0;
    internal static bool IsNonSpacingMark(Rune rune) => (Properties(rune) & 4) != 0;
    internal static bool IsWhiteSpace(Rune rune) => (Properties(rune) & 8) != 0;
    internal static string? Lowercase(Rune rune) => lower.GetValueOrDefault(rune.Value);
    internal static bool HasCanonicalNonSpacingMark(Rune rune) => HasCanonicalNonSpacingMark(rune.Value);
    private static bool HasCanonicalNonSpacingMark(int code)
    {
        if (code < 0xC0) return false;
        if ((Lookup(properties, code) & 4) != 0) return true;
        if (decompositions.TryGetValue(code, out var decomposition) && !decomposition.Compatibility)
            foreach (int part in decomposition.Scalars)
                if (HasCanonicalNonSpacingMark(part)) return true;
        return false;
    }

    internal static void ValidateUtf16(string text)
    {
        for (int position = 0; position < text.Length; position++)
        {
            if (!char.IsSurrogate(text[position])) continue;
            if (position + 1 >= text.Length || !char.IsSurrogatePair(text[position], text[position + 1]))
                throw new ArgumentException("Text contains invalid UTF-16", nameof(text));
            position++;
        }
    }

    internal static bool IsWhiteSpace(string text)
    {
        foreach (Rune rune in text.EnumerateRunes()) if (!IsWhiteSpace(rune)) return false;
        return true;
    }

    internal static string TrimWhiteSpace(string text)
    {
        int start = 0, end = text.Length;
        while (start < end && Rune.DecodeFromUtf16(text.AsSpan(start, end - start), out Rune first, out int size) == OperationStatus.Done && IsWhiteSpace(first))
            start += size;
        while (end > start && Rune.DecodeLastFromUtf16(text.AsSpan(start, end - start), out Rune last, out int size) == OperationStatus.Done && IsWhiteSpace(last))
            end -= size;
        return text[start..end];
    }

    private static byte QuickMask(NormalizationForm form) => form switch
    {
        NormalizationForm.FormD => 1, NormalizationForm.FormKD => 2,
        NormalizationForm.FormC => 4, NormalizationForm.FormKC => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(form))
    };

    // A Maybe result takes the full path. Continue validating UTF-16 after finding a change.
    internal static bool CanSkipNormalization(string text, NormalizationForm form)
    {
        byte mask = QuickMask(form), previous = 0;
        bool normalized = true;
        for (int position = 0; position < text.Length;)
        {
            int code;
            if (text[position] < 128) code = text[position++];
            else
            {
                if (Rune.DecodeFromUtf16(text.AsSpan(position), out Rune rune, out int consumed) != OperationStatus.Done)
                    throw new ArgumentException("Text contains invalid UTF-16", nameof(text));
                code = rune.Value;
                position += consumed;
            }
            if (code < 128) { previous = 0; continue; }
            byte current = Lookup(combining, code);
            if ((Lookup(quick, code) & mask) != 0 || current != 0 && previous > current) normalized = false;
            previous = current;
        }
        return normalized;
    }

    internal static string Normalize(string text, NormalizationForm form) =>
        CanSkipNormalization(text, form) ? text : Normalize(text, form, null, out _);

    internal static string Normalize(string text, NormalizationForm form, Func<uint[][]>? getReferences, out uint[][]? mapped)
    {
        bool compatibility = form is NormalizationForm.FormKC or NormalizationForm.FormKD;
        var units = new List<Unit>(text.Length);
        List<(int Left, int Right)>? mergedOrigins = null;
        void Add(int code, int origin)
        {
            const int sBase = 0xAC00, lBase = 0x1100, vBase = 0x1161, tBase = 0x11A7;
            int syllable = code - sBase;
            if (syllable is >= 0 and < 11172)
            {
                Add(lBase + syllable / 588, origin);
                Add(vBase + syllable % 588 / 28, origin);
                if (syllable % 28 != 0) Add(tBase + syllable % 28, origin);
            }
            else if (decompositions.TryGetValue(code, out var decomposition) && (compatibility || !decomposition.Compatibility))
                foreach (int part in decomposition.Scalars) Add(part, origin);
            else units.Add(new(code, code < 128 ? (byte)0 : Lookup(combining, code), units.Count, origin));
        }
        int scalar = 0;
        foreach (Rune rune in text.EnumerateRunes()) Add(rune.Value, scalar++);
        // Stable ordering within nonstarter runs. Sort only an actually disordered run;
        // unlike insertion sorting, adversarial combining sequences remain O(n log n).
        int start = 0;
        bool ordered = true;
        for (int i = 0; i <= units.Count; i++)
        {
            if (i == units.Count || units[i].Combining == 0)
            {
                if (!ordered) units.Sort(start, i - start, canonicalOrder);
                start = i + 1;
                ordered = true;
            }
            else if (i > start && units[i - 1].Combining > units[i].Combining) ordered = false;
        }
        if (form is NormalizationForm.FormC or NormalizationForm.FormKC)
        {
            int starter = -1, written = 0;
            byte previous = 0;
            for (int i = 0; i < units.Count; i++)
            {
                Unit current = units[i];
                int composed = starter >= 0 && (previous == 0 || previous < current.Combining)
                    ? Compose(units[starter].Scalar, current.Scalar) : -1;
                if (composed >= 0)
                {
                    Unit first = units[starter];
                    int origin = first.Origin;
                    if (getReferences != null && origin != current.Origin)
                    {
                        mergedOrigins ??= new();
                        mergedOrigins.Add((origin, current.Origin));
                        origin = -mergedOrigins.Count;
                    }
                    units[starter] = first with { Scalar = composed, Origin = origin };
                }
                else
                {
                    units[written] = current;
                    if (current.Combining == 0) starter = written;
                    written++;
                    previous = current.Combining;
                }
            }
            if (written < units.Count) units.RemoveRange(written, units.Count - written);
        }
        mapped = null;
        bool unchanged = units.Count == scalar;
        if (unchanged)
        {
            int i = 0;
            foreach (Rune rune in text.EnumerateRunes())
                if (units[i++].Scalar != rune.Value) { unchanged = false; break; }
        }
        if (unchanged) return text;

        // Carry source indices through normalization. Materialize ownership metadata
        // only after a text change is established, including legitimate Maybe inputs.
        if (getReferences != null)
        {
            uint[][] references = getReferences();
            var merged = new uint[mergedOrigins?.Count ?? 0][];
            uint[] Resolve(int origin) => origin >= 0 ? references[origin] : merged[-origin - 1];
            for (int i = 0; i < merged.Length; i++)
            {
                var pair = mergedOrigins![i];
                merged[i] = TokenAlignment.SourcePositions([Resolve(pair.Left), Resolve(pair.Right)]);
            }
            mapped = new uint[units.Count][];
            for (int i = 0; i < units.Count; i++) mapped[i] = Resolve(units[i].Origin);
        }
        var result = new StringBuilder(text.Length);
        for (int i = 0; i < units.Count; i++)
        {
            result.AppendRune(new Rune(units[i].Scalar));
        }
        return result.ToString();
    }

    private static long Key(int left, int right) => ((long)left << 21) | (uint)right;
    private static int Compose(int left, int right)
    {
        int l = left - 0x1100, v = right - 0x1161;
        if (l is >= 0 and < 19 && v is >= 0 and < 21) return 0xAC00 + (l * 21 + v) * 28;
        int s = left - 0xAC00, t = right - 0x11A7;
        if (s is >= 0 and < 11172 && s % 28 == 0 && t is > 0 and < 28) return left + t;
        return compositions.GetValueOrDefault(Key(left, right), -1);
    }
}

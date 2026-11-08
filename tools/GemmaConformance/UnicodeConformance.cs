using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lokad.Tokenizers.Tokenizer;

internal static class UnicodeConformance
{
    internal static void Run(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Expected Unicode reference JSON and report path");
        using var reference = JsonDocument.Parse(File.ReadAllBytes(args[0]));
        var root = reference.RootElement;
        if (root.GetProperty("unicodeVersion").GetString() != UnicodeProfile.Version) throw new InvalidDataException("Unicode version mismatch");
        var cases = root.GetProperty("cases").EnumerateArray().ToDictionary(row => row[0].GetInt32(), row =>
            row.EnumerateArray().Skip(1).Select(value => value.GetString()!).ToArray());
        var properties = new byte[0x110000];
        foreach (var range in root.GetProperty("properties").EnumerateArray())
            properties.AsSpan(range[0].GetInt32(), range[1].GetInt32()-range[0].GetInt32()+1).Fill(range[2].GetByte());
        NormalizationForm[] forms = [NormalizationForm.FormC, NormalizationForm.FormD, NormalizationForm.FormKC, NormalizationForm.FormKD];
        int count = 0;
        for (int code = 0; code <= 0x10FFFF; code++)
        {
            if (!Rune.IsValid(code)) continue;
            var rune = new Rune(code);
            string text = rune.ToString();
            cases.TryGetValue(code, out var expected);
            if ((UnicodeProfile.Lowercase(rune) ?? text) != (expected?[0] ?? text) || UnicodeProfile.Properties(rune) != properties[code])
                throw new InvalidDataException($"Unicode casing/classification mismatch U+{code:X}");
            for (int i = 0; i < forms.Length; i++)
                if (UnicodeProfile.Normalize(text, forms[i]) != (expected?[i+1] ?? text))
                    throw new InvalidDataException($"Unicode {forms[i]} mismatch U+{code:X}");
            count++;
        }
        if (count != root.GetProperty("scalars").GetInt32()) throw new InvalidDataException("Unicode coverage mismatch");
        string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
        File.WriteAllText(args[1], JsonSerializer.Serialize(new { unicodeVersion = UnicodeProfile.Version, scalars = count,
            normalizationChecks = count * 4, referenceSha256 = Hash(args[0]), librarySha256 = Hash(typeof(GemmaTokenizer).Assembly.Location) }));
        Console.WriteLine($"PASS exhaustive Unicode {UnicodeProfile.Version}: {count} scalars, casing/classification and all four normalization forms");
    }
}

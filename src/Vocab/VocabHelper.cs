using System.Text.Json;
using System.Text;
using Lokad.Tokenizers.Exceptions;
using Lokad.Tokenizers.Tokenizer;

namespace Lokad.Tokenizers.Vocab;

/// <summary>Local vocabulary and special-token mapping readers.</summary>
public static class VocabHelper
{
    public static Dictionary<TValue, TKey> SwapKeyValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        where TKey : notnull where TValue : notnull => dictionary.ToDictionary(p => p.Value, p => p.Key);

    /// <summary>Read strict UTF-8, or BOM-identified UTF-8/UTF-16/UTF-32, one token per line.</summary>
    public static Dictionary<string, long> ReadFlatFile(string path)
    {
        CheckFile(path);
        using var stream = File.OpenRead(path);
        Span<byte> prefix = stackalloc byte[4];
        int size = stream.ReadAtLeast(prefix, 4, throwOnEndOfStream: false), preamble = 0;
        Encoding encoding = new UTF8Encoding(false, true);
        if (size >= 4 && prefix[..4].SequenceEqual<byte>([0xFF,0xFE,0,0]))
        { encoding = new UTF32Encoding(false, false, true); preamble = 4; }
        else if (size >= 4 && prefix[..4].SequenceEqual<byte>([0,0,0xFE,0xFF]))
        { encoding = new UTF32Encoding(true, false, true); preamble = 4; }
        else if (size >= 3 && prefix[..3].SequenceEqual<byte>([0xEF,0xBB,0xBF])) preamble = 3;
        else if (size >= 2 && prefix[..2].SequenceEqual<byte>([0xFF,0xFE]))
        { encoding = new UnicodeEncoding(false, false, true); preamble = 2; }
        else if (size >= 2 && prefix[..2].SequenceEqual<byte>([0xFE,0xFF]))
        { encoding = new UnicodeEncoding(true, false, true); preamble = 2; }
        stream.Position = preamble;
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false);
        var values = new Dictionary<string, long>(StringComparer.Ordinal);
        try
        {
            while (reader.ReadLine() is { } token) values.Add(token, values.Count);
        }
        catch (DecoderFallbackException error)
        { throw new InvalidDataException("Invalid " + encoding.WebName + " vocabulary text in " + path, error); }
        return values;
    }

    /// <summary>Read nonempty token names and unique nonnegative Int64 IDs; duplicate names are rejected.</summary>
    public static Dictionary<string, long> ReadJsonFile(string path)
    {
        CheckFile(path);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Vocabulary JSON must be an object");
            var values = new Dictionary<string, long>(StringComparer.Ordinal);
            var ids = new HashSet<long>();
            foreach (var entry in document.RootElement.EnumerateObject())
            {
                string name = JsonAsset.Name(entry, "vocabulary");
                if (name.Length == 0 || entry.Value.ValueKind != JsonValueKind.Number ||
                    !entry.Value.TryGetInt64(out long id) || id < 0)
                    throw new InvalidDataException("Invalid vocabulary name/ID: " + name);
                if (!values.TryAdd(name, id))
                    throw new InvalidDataException("Duplicate vocabulary token: " + name);
                if (!ids.Add(id)) throw new InvalidDataException("Duplicate vocabulary ID: " + id);
            }
            return values;
        }
        catch (JsonException error) { throw new InvalidDataException("Invalid vocabulary JSON at " + error.Path, error); }
    }

    public static ModelProto OpenProtobufFile(string path)
    {
        CheckFile(path);
        using var stream = File.OpenRead(path);
        return ModelProto.Parser.ParseFrom(stream);
    }

    public static Dictionary<string, long> ReadProtobufFile(string path) =>
        OpenProtobufFile(path).Pieces.Select((piece, id) => (piece, id))
            .ToDictionary(p => p.piece.Piece, p => (long)p.id, StringComparer.Ordinal);

    /// <summary>Read snake_case mapping names, or their legacy PascalCase equivalents.</summary>
    /// <remarks>Token values can be strings or content objects with literal matching flags.</remarks>
    public static SpecialTokenMap ReadSpecialTokenMappingFile(string path) => ReadSpecialTokenMappingFile(path, "[UNK]");

    public static SpecialTokenMap ReadSpecialTokenMappingFile(string path, string defaultUnknownToken)
    {
        CheckFile(path);
        ArgumentException.ThrowIfNullOrEmpty(defaultUnknownToken);
        using var document = JsonAsset.Parse(File.ReadAllBytes(path));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Special-token mapping must be a JSON object");
        var result = new SpecialTokenMap { UnkToken = defaultUnknownToken };
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            string rawName = JsonAsset.Name(property, "special_tokens");
            string name = rawName switch
            {
                "UnkToken" => "unk_token", "PadToken" => "pad_token", "BosToken" => "bos_token",
                "SepToken" => "sep_token", "ClsToken" => "cls_token", "EosToken" => "eos_token",
                "MaskToken" => "mask_token", "AdditionalSpecialTokens" => "additional_special_tokens",
                _ => rawName
            };
            if (!seen.Add(name)) throw new InvalidDataException("Duplicate special-token field: " + name);
            if (name == "additional_special_tokens")
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException("additional_special_tokens must be an array");
                foreach (JsonElement entry in property.Value.EnumerateArray())
                    if (!result.AdditionalSpecialTokens.Add(ReadToken(entry, "additional_special_tokens[]")
                        ?? throw new InvalidDataException("Additional tokens must not be null")))
                        throw new InvalidDataException("Duplicate additional special token");
                continue;
            }
            string? token = ReadToken(property.Value, name);
            switch (name)
            {
                case "unk_token": result.UnkToken = token ?? throw new InvalidDataException("Unknown token must not be null"); break;
                case "pad_token": result.PadToken = token; break;
                case "bos_token": result.BosToken = token; break;
                case "sep_token": result.SepToken = token; break;
                case "cls_token": result.ClsToken = token; break;
                case "eos_token": result.EosToken = token; break;
                case "mask_token": result.MaskToken = token; break;
                default: throw new InvalidDataException("Unknown special-token field: " + rawName);
            }
        }
        return result;
    }

    private static string? ReadToken(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Null) return null;
        if (element.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var flag in element.EnumerateObject())
            {
                string name = JsonAsset.Name(flag, path);
                if (!seen.Add(name)) throw new InvalidDataException("Duplicate token object field: " + path + "." + name);
                if (name is "content" or "__type") continue;
                if (name is "single_word" or "lstrip" or "rstrip" or "normalized")
                {
                    if (flag.Value.ValueKind != JsonValueKind.False)
                        throw new NotSupportedException("Only literal special-token matching is supported: " + name);
                }
                else if (name != "special" || flag.Value.ValueKind != JsonValueKind.True)
                    throw new InvalidDataException("Unsupported token object field: " + name);
            }
            if (!element.TryGetProperty("content", out element))
                throw new InvalidDataException("A token object must have content");
            path += ".content";
        }
        return JsonAsset.String(element, path);
    }

    public static void RegisterAsSpecialValue(string token, Dictionary<string, long> values, Dictionary<string, long> specialValues)
    {
        if (!values.TryGetValue(token, out long id))
            throw new TokenNotFoundTokenizerException("Unknown token " + token + " not found");
        specialValues[token] = id;
    }

    private static void CheckFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path)) throw new FileNotFoundTokenizerException(path + " vocabulary file not found");
    }
}

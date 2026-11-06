using System.Text.Json;
using Lokad.Tokenizers.Exceptions;

namespace Lokad.Tokenizers.Vocab;

/// <summary>Local vocabulary and special-token mapping readers.</summary>
public static class VocabHelper
{
    public static Dictionary<TValue, TKey> SwapKeyValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        where TKey : notnull where TValue : notnull => dictionary.ToDictionary(p => p.Value, p => p.Key);

    public static Dictionary<string, long> ReadFlatFile(string path)
    {
        CheckFile(path);
        return File.ReadAllLines(path).Select((text, id) => (text, id))
            .ToDictionary(p => p.text, p => (long)p.id, StringComparer.Ordinal);
    }

    public static Dictionary<string, long> ReadJsonFile(string path)
    {
        CheckFile(path);
        return JsonSerializer.Deserialize<Dictionary<string, long>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Vocabulary JSON must be an object");
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
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Special-token mapping must be a JSON object");
        var result = new SpecialTokenMap { UnkToken = defaultUnknownToken };
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            string name = property.Name switch
            {
                "UnkToken" => "unk_token", "PadToken" => "pad_token", "BosToken" => "bos_token",
                "SepToken" => "sep_token", "ClsToken" => "cls_token", "EosToken" => "eos_token",
                "MaskToken" => "mask_token", "AdditionalSpecialTokens" => "additional_special_tokens",
                _ => property.Name
            };
            if (!seen.Add(name)) throw new InvalidDataException("Duplicate special-token field: " + name);
            if (name == "additional_special_tokens")
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException("additional_special_tokens must be an array");
                foreach (JsonElement entry in property.Value.EnumerateArray())
                    if (!result.AdditionalSpecialTokens.Add(ReadToken(entry)
                        ?? throw new InvalidDataException("Additional tokens must not be null")))
                        throw new InvalidDataException("Duplicate additional special token");
                continue;
            }
            string? token = ReadToken(property.Value);
            switch (name)
            {
                case "unk_token": result.UnkToken = token ?? throw new InvalidDataException("Unknown token must not be null"); break;
                case "pad_token": result.PadToken = token; break;
                case "bos_token": result.BosToken = token; break;
                case "sep_token": result.SepToken = token; break;
                case "cls_token": result.ClsToken = token; break;
                case "eos_token": result.EosToken = token; break;
                case "mask_token": result.MaskToken = token; break;
                default: throw new InvalidDataException("Unknown special-token field: " + property.Name);
            }
        }
        return result;
    }

    private static string? ReadToken(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null) return null;
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var flag in element.EnumerateObject())
            {
                if (flag.Name is "content" or "__type") continue;
                if (flag.Name is "single_word" or "lstrip" or "rstrip" or "normalized")
                {
                    if (flag.Value.ValueKind != JsonValueKind.False)
                        throw new NotSupportedException("Only literal special-token matching is supported: " + flag.Name);
                }
                else if (flag.Name != "special" || flag.Value.ValueKind != JsonValueKind.True)
                    throw new InvalidDataException("Unsupported token object field: " + flag.Name);
            }
            if (!element.TryGetProperty("content", out element))
                throw new InvalidDataException("A token object must have content");
        }
        if (element.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(element.GetString()))
            throw new InvalidDataException("A special token must be a nonempty string");
        return element.GetString();
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

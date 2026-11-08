using System.Text.Json;

namespace Lokad.Tokenizers.Tokenizer;

internal static class JsonAsset
{
    internal static JsonDocument Parse(byte[] data)
    {
        try { return JsonDocument.Parse(data); }
        catch (JsonException error) { throw new InvalidDataException("Invalid tokenizer JSON at " + error.Path, error); }
    }

    internal static JsonElement Object(JsonElement value, string path, bool checkDuplicates = true)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid(path, "an object");
        if (checkDuplicates)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                string name = Name(property, path);
                if (!seen.Add(name)) throw new InvalidDataException("Duplicate tokenizer field: " + path + "." + name);
            }
        }
        return value;
    }

    internal static JsonElement Required(JsonElement parent, string name, string path) =>
        parent.TryGetProperty(name, out var value) ? value : throw new InvalidDataException("Missing tokenizer field: " + path + "." + name);

    internal static JsonElement Array(JsonElement value, string path) =>
        value.ValueKind == JsonValueKind.Array ? value : throw Invalid(path, "an array");

    internal static string Name(JsonProperty property, string path)
    {
        try { return property.Name; }
        catch (InvalidOperationException error) { throw InvalidUnicode(path + ".<property name>", error); }
    }

    internal static string String(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String) throw Invalid(path, "a nonempty string");
        string? text;
        // JsonDocument defers decoding escaped UTF-16 until GetString/Name.
        // The kind check keeps unrelated wrong-kind exceptions outside this translation.
        try { text = value.GetString(); }
        catch (InvalidOperationException error) { throw InvalidUnicode(path, error); }
        return text is { Length: > 0 } ? text : throw Invalid(path, "a nonempty string");
    }

    internal static string String(JsonElement parent, string name, string path)
    {
        return String(Required(parent, name, path), path + "." + name);
    }

    internal static int Int(JsonElement value, string path) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) ? number : throw Invalid(path, "a 32-bit integer");

    internal static bool Bool(JsonElement parent, string name, string path)
    {
        var value = Required(parent, name, path);
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False) throw Invalid(path + "." + name, "a boolean");
        return value.GetBoolean();
    }

    private static InvalidDataException Invalid(string path, string expected) => new("Tokenizer field " + path + " must be " + expected);
    private static InvalidDataException InvalidUnicode(string path, Exception error) => new("Invalid Unicode string at " + path, error);
}

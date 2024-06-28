using System.Text.Json;
using Lokad.Tokenizers.Exceptions;

namespace Lokad.Tokenizers.Vocab;

public static class VocabHelper
{
    public static Dictionary<TValue, TKey> SwapKeyValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        return dictionary.ToDictionary((i) => i.Value, (i) => i.Key);
    }

    public static Dictionary<string, long> ReadFlatFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        var lines = File.ReadAllLines(path);
        return lines.Select((line, index) => new { line, index })
                    .ToDictionary(x => x.line.Trim(), x => (long)x.index);
    }

    public static Dictionary<string, long> ReadJsonFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, long>>(json);
    }

    public static ModelProto OpenProtobufFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        using var stream = File.OpenRead(path);

        return ModelProto.Parser.ParseFrom(stream);
    }

    public static Dictionary<string, long> ReadProtobufFile(string path)
    {
        var proto = OpenProtobufFile(path);
        return proto.Pieces
                    .Select((piece, idx) => new { piece, idx })
                    .ToDictionary(x => x.piece.Piece, x => (long)x.idx);
    }

    public static SpecialTokenMap ReadSpecialTokenMappingFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SpecialTokenMap>(json);
    }

    public static void RegisterAsSpecialValue(string token, Dictionary<string, long> values, Dictionary<string, long> specialValues)
    {
        if (!values.TryGetValue(token, out var tokenIndex))
            throw new TokenNotFoundTokenizerException($"Unknown token {token} not found");

        specialValues[token] = tokenIndex;
    }
}

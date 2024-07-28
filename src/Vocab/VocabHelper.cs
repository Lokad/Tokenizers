using System.Text.Json;
using Lokad.Tokenizers.Exceptions;

namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Provides helper methods for working with vocabularies.
/// </summary>
public static class VocabHelper
{
    /// <summary>
    /// Swaps the keys and values in the given dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to swap keys and values for.</param>
    /// <returns>A new dictionary with keys and values swapped.</returns>
    public static Dictionary<TValue, TKey> SwapKeyValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        return dictionary.ToDictionary((i) => i.Value, (i) => i.Key);
    }

    /// <summary>
    /// Reads a flat file and returns a dictionary mapping each line to its index.
    /// </summary>
    /// <param name="path">The path to the flat file.</param>
    /// <returns>A dictionary mapping each line to its index.</returns>
    /// <exception cref="FileNotFoundTokenizerException">Thrown when the file is not found.</exception>
    public static Dictionary<string, long> ReadFlatFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        var lines = File.ReadAllLines(path);
        return lines.Select((line, index) => new { line, index })
                    .ToDictionary(x => x.line.Trim(), x => (long)x.index);
    }

    /// <summary>
    /// Reads a JSON file and returns a dictionary.
    /// </summary>
    /// <param name="path">The path to the JSON file.</param>
    /// <returns>A dictionary deserialized from the JSON file.</returns>
    /// <exception cref="FileNotFoundTokenizerException">Thrown when the file is not found.</exception>
    public static Dictionary<string, long> ReadJsonFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, long>>(json);
    }

    /// <summary>
    /// Opens a protobuf file and returns a <see cref="ModelProto"/> instance.
    /// </summary>
    /// <param name="path">The path to the protobuf file.</param>
    /// <returns>A <see cref="ModelProto"/> instance deserialized from the protobuf file.</returns>
    /// <exception cref="FileNotFoundTokenizerException">Thrown when the file is not found.</exception>
    public static ModelProto OpenProtobufFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        using var stream = File.OpenRead(path);

        return ModelProto.Parser.ParseFrom(stream);
    }

    /// <summary>
    /// Reads a protobuf file and returns a dictionary mapping pieces to their indices.
    /// </summary>
    /// <param name="path">The path to the protobuf file.</param>
    /// <returns>A dictionary mapping pieces to their indices.</returns>
    public static Dictionary<string, long> ReadProtobufFile(string path)
    {
        var proto = OpenProtobufFile(path);
        return proto.Pieces
                    .Select((piece, idx) => new { piece, idx })
                    .ToDictionary(x => x.piece.Piece, x => (long)x.idx);
    }

    /// <summary>
    /// Reads a special token mapping file and returns a <see cref="SpecialTokenMap"/> instance.
    /// </summary>
    /// <param name="path">The path to the special token mapping file.</param>
    /// <returns>A <see cref="SpecialTokenMap"/> instance deserialized from the file.</returns>
    /// <exception cref="FileNotFoundTokenizerException">Thrown when the file is not found.</exception>
    public static SpecialTokenMap ReadSpecialTokenMappingFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundTokenizerException($"{path} vocabulary file not found");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SpecialTokenMap>(json);
    }

    /// <summary>
    /// Registers a token as a special value in the provided dictionaries.
    /// </summary>
    /// <param name="token">The token to register.</param>
    /// <param name="values">The dictionary of token values and their corresponding IDs.</param>
    /// <param name="specialValues">The dictionary of special token values and their corresponding IDs.</param>
    /// <exception cref="TokenNotFoundTokenizerException">Thrown when the token is not found in the values dictionary.</exception>
    public static void RegisterAsSpecialValue(string token, Dictionary<string, long> values, Dictionary<string, long> specialValues)
    {
        if (!values.TryGetValue(token, out var tokenIndex))
            throw new TokenNotFoundTokenizerException($"Unknown token {token} not found");

        specialValues[token] = tokenIndex;
    }
}

using System.Text.Json;
using Google.Protobuf;

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

        string json = File.ReadAllText(path);
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

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SpecialTokenMap>(json);
    }

    public static void RegisterAsSpecialValue(string token, Dictionary<string, long> values, Dictionary<string, long> specialValues)
    {
        if (!values.TryGetValue(token, out long tokenIndex))
            throw new TokenNotFoundTokenizerException($"Unknown token {token} not found");

        specialValues[token] = tokenIndex;
    }
}

public class SpecialTokenMap
{
    private const string DefaultUnkToken = "[UNK]";

    public string UnkToken { get; set; }
    public string PadToken { get; set; }
    public string BosToken { get; set; }
    public string SepToken { get; set; }
    public string ClsToken { get; set; }
    public string EosToken { get; set; }
    public string MaskToken { get; set; }
    public HashSet<string> AdditionalSpecialTokens { get; set; }

    public SpecialTokenMap()
    {
        UnkToken = DefaultUnkToken;
        AdditionalSpecialTokens = new HashSet<string>();
    }

    public void RegisterSpecialValues(Dictionary<string, long> values, Dictionary<string, long> specialValues)
    {
        // Register the unk_token as a special value
        if (!string.IsNullOrEmpty(UnkToken))
        {
            VocabHelper.RegisterAsSpecialValue(UnkToken, values, specialValues);
        }

        // Register other special tokens if they are not null or empty
        if (!string.IsNullOrEmpty(PadToken))
        {
            VocabHelper.RegisterAsSpecialValue(PadToken, values, specialValues);
        }

        if (!string.IsNullOrEmpty(BosToken))
        {
            VocabHelper.RegisterAsSpecialValue(BosToken, values, specialValues);
        }

        if (!string.IsNullOrEmpty(SepToken))
        {
            VocabHelper.RegisterAsSpecialValue(SepToken, values, specialValues);
        }

        if (!string.IsNullOrEmpty(ClsToken))
        {
            VocabHelper.RegisterAsSpecialValue(ClsToken, values, specialValues);
        }

        if (!string.IsNullOrEmpty(EosToken))
        {
            VocabHelper.RegisterAsSpecialValue(EosToken, values, specialValues);
        }

        if (!string.IsNullOrEmpty(MaskToken))
        {
            VocabHelper.RegisterAsSpecialValue(MaskToken, values, specialValues);
        }

        // Register any additional special tokens
        if (AdditionalSpecialTokens != null)
        {
            foreach (var token in AdditionalSpecialTokens)
            {
                if (!string.IsNullOrEmpty(token))
                {
                    VocabHelper.RegisterAsSpecialValue(token, values, specialValues);
                }
            }
        }
    }
}

public interface IVocab
{
    string GetUnknownValue();
    Dictionary<string, long> Values { get; }
    Dictionary<long, string> Indices { get; }
    Dictionary<string, long> SpecialValues { get; }
    Dictionary<long, string> SpecialIndices { get; }

    IEnumerable<string> SpecialTokens();

    long TokenToId(string token);
    string IdToToken(long id);
    List<long> ConvertTokensToIds(IEnumerable<string> tokens);
    void AddExtraIds(long numExtraIds);
    void AddTokens(IEnumerable<string> tokens);
}

public class BaseVocab : IVocab
{
    public Dictionary<string, long> Values { get; private set; }
    public Dictionary<long, string> Indices { get; private set; }
    public SpecialTokenMap SpecialTokenMap { get; private set; }
    public Dictionary<string, long> SpecialValues { get; private set; }
    public Dictionary<long, string> SpecialIndices { get; private set; }

    public BaseVocab(Dictionary<string, long> values, SpecialTokenMap specialTokenMap)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
        SpecialTokenMap = specialTokenMap ?? throw new ArgumentNullException(nameof(specialTokenMap));
        SpecialValues = new Dictionary<string, long>();
        SpecialTokenMap.RegisterSpecialValues(values, SpecialValues);
        Indices = VocabHelper.SwapKeyValue(Values);
        SpecialIndices = VocabHelper.SwapKeyValue(SpecialValues);
    }

    public string GetUnknownValue() => SpecialTokenMap.UnkToken;

    public IEnumerable<string> SpecialTokens()
    {
        // [vermorel] Method manually inserted, not sure about the semantic.
        return SpecialValues.Keys;
    }

    public long TokenToId(string token)
    {
        if (SpecialValues.TryGetValue(token, out var id))
            return id;
        return Values.TryGetValue(token, out id) ? id : Values[GetUnknownValue()];
    }

    public string IdToToken(long id)
    {
        if (SpecialIndices.TryGetValue(id, out string token))
            return token;
        return Indices.TryGetValue(id, out token) ? token : GetUnknownValue();
    }

    public List<long> ConvertTokensToIds(IEnumerable<string> tokens)
    {
        return tokens.Select(TokenToId).ToList();
    }

    public void AddExtraIds(long numExtraIds)
    {
        // Implementation for adding extra IDs...
        var maxId = Values.Values.DefaultIfEmpty().Max();
        for (long i = 0; i < numExtraIds; i++)
        {
            var extraIdToken = $"<extra_id_{i}>";
            var nextId = maxId + i + 1;
            Values[extraIdToken] = nextId;
            Indices[nextId] = extraIdToken;
        }
    }

    public void AddTokens(IEnumerable<string> tokens)
    {
        // Implementation for adding tokens...
        foreach (var token in tokens)
        {
            if (!Values.ContainsKey(token))
            {
                var nextId = Values.Values.DefaultIfEmpty().Max() + 1;
                Values[token] = nextId;
                Indices[nextId] = token;
            }
        }
    }
}
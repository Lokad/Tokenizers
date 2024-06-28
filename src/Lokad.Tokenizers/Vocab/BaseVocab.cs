using Google.Protobuf;

namespace Lokad.Tokenizers.Vocab;

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
        if (SpecialIndices.TryGetValue(id, out var token))
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
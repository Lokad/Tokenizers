using Google.Protobuf;

namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Represents a base vocabulary for tokenization.
/// </summary>
public class BaseVocab : IVocab
{
    /// <summary>
    /// Gets the dictionary of token values and their corresponding IDs.
    /// </summary>
    public Dictionary<string, long> Values { get; private set; }

    /// <summary>
    /// Gets the dictionary of token IDs and their corresponding values.
    /// </summary>
    public Dictionary<long, string> Indices { get; private set; }

    /// <summary>
    /// Gets the special token map.
    /// </summary>
    public SpecialTokenMap SpecialTokenMap { get; private set; }

    /// <summary>
    /// Gets the dictionary of special token values and their corresponding IDs.
    /// </summary>
    public Dictionary<string, long> SpecialValues { get; private set; }

    /// <summary>
    /// Gets the dictionary of special token IDs and their corresponding values.
    /// </summary>
    public Dictionary<long, string> SpecialIndices { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseVocab"/> class.
    /// </summary>
    /// <param name="values">The dictionary of token values and their corresponding IDs.</param>
    /// <param name="specialTokenMap">The special token map.</param>
    /// <exception cref="ArgumentNullException">Thrown when values or specialTokenMap is null.</exception>
    public BaseVocab(Dictionary<string, long> values, SpecialTokenMap specialTokenMap)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
        SpecialTokenMap = specialTokenMap ?? throw new ArgumentNullException(nameof(specialTokenMap));
        SpecialValues = new Dictionary<string, long>();
        SpecialTokenMap.RegisterSpecialValues(values, SpecialValues);
        Indices = VocabHelper.SwapKeyValue(Values);
        SpecialIndices = VocabHelper.SwapKeyValue(SpecialValues);
    }

    /// <summary>
    /// Gets the value representing an unknown token.
    /// </summary>
    /// <returns>The unknown token value.</returns>
    public string GetUnknownValue() => SpecialTokenMap.UnkToken;

    /// <summary>
    /// Gets the value representing the beginning of a sequence.
    /// </summary>
    /// <returns>The beginning of sequence token value.</returns>
    public string GetBosValue() => SpecialTokenMap.BosToken;

    /// <summary>
    /// Gets the value representing the end of a sequence.
    /// </summary>
    /// <returns>The end of sequence token value.</returns>
    public string GetEosValue() => SpecialTokenMap.EosToken;

    /// <summary>
    /// Gets the value representing a padding token.
    /// </summary>
    /// <returns>The padding token value.</returns>
    public string GetPadValue() => SpecialTokenMap.PadToken;

    /// <summary>
    /// Gets the value representing a separator token.
    /// </summary>
    /// <returns>The separator token value.</returns>
    public string GetSepValue() => SpecialTokenMap.SepToken;

    /// <summary>
    /// Gets the value representing a classification token.
    /// </summary>
    /// <returns>The classification token value.</returns>
    public string GetClsValue() => SpecialTokenMap.ClsToken;

    /// <summary>
    /// Gets the value representing a mask token.
    /// </summary>
    /// <returns>The mask token value.</returns>
    public string GetMaskValue() => SpecialTokenMap.MaskToken;

    /// <summary>
    /// Gets the collection of special tokens.
    /// </summary>
    /// <returns>An enumerable collection of special tokens.</returns>
    public IEnumerable<string> SpecialTokens()
    {
        // [vermorel] Method manually inserted, not sure about the semantic.
        return SpecialValues.Keys;
    }

    /// <summary>
    /// Converts a token to its corresponding ID.
    /// </summary>
    /// <param name="token">The token to convert.</param>
    /// <returns>The ID of the token.</returns>
    public long TokenToId(string token)
    {
        if (SpecialValues.TryGetValue(token, out var id))
            return id;
        return Values.TryGetValue(token, out id) ? id : Values[GetUnknownValue()];
    }

    /// <summary>
    /// Converts an ID to its corresponding token.
    /// </summary>
    /// <param name="id">The ID to convert.</param>
    /// <returns>The token corresponding to the ID.</returns>
    public string IdToToken(long id)
    {
        if (SpecialIndices.TryGetValue(id, out var token))
            return token;
        return Indices.TryGetValue(id, out token) ? token : GetUnknownValue();
    }

    /// <summary>
    /// Converts a collection of tokens to their corresponding IDs.
    /// </summary>
    /// <param name="tokens">The tokens to convert.</param>
    /// <returns>A list of IDs corresponding to the tokens.</returns>
    public List<long> ConvertTokensToIds(IEnumerable<string> tokens)
    {
        return tokens.Select(TokenToId).ToList();
    }

    /// <summary>
    /// Adds extra IDs to the vocabulary.
    /// </summary>
    /// <param name="numExtraIds">The number of extra IDs to add.</param>
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

    /// <summary>
    /// Adds a collection of tokens to the vocabulary.
    /// </summary>
    /// <param name="tokens">The tokens to add.</param>
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

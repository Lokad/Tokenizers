using System.Collections.ObjectModel;

namespace Lokad.Tokenizers.Vocab;

/// <summary>A vocabulary with read-only views and controlled token additions.</summary>
/// <remarks>Concurrent reads are supported. Do not add tokens during tokenization.</remarks>
public class BaseVocab : IVocab
{
    private readonly Dictionary<string, long> values;
    private readonly Dictionary<long, string> indices;
    private readonly SpecialTokenMap specialTokens;
    private readonly object mutation = new();
    public IReadOnlyDictionary<string, long> Values { get; }
    public IReadOnlyDictionary<long, string> Indices { get; }
    /// <summary>A copy of the special-token configuration.</summary>
    public SpecialTokenMap SpecialTokenMap => specialTokens.Copy();
    public IReadOnlyDictionary<string, long> SpecialValues { get; }
    public IReadOnlyDictionary<long, string> SpecialIndices { get; }

    public BaseVocab(Dictionary<string, long> values, SpecialTokenMap specialTokenMap)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(specialTokenMap);
        if (values.Any(p => string.IsNullOrEmpty(p.Key) || p.Value < 0))
            throw new ArgumentException("Vocabulary tokens must be nonempty and IDs nonnegative", nameof(values));
        this.values = new(values, StringComparer.Ordinal);
        if (values.Values.Distinct().Count() != values.Count)
            throw new ArgumentException("Vocabulary IDs must be unique", nameof(values));
        indices = this.values.ToDictionary(p => p.Value, p => p.Key);
        specialTokens = specialTokenMap.Copy();
        var specials = new Dictionary<string, long>(StringComparer.Ordinal);
        specialTokens.RegisterSpecialValues(this.values, specials);
        Values = new ReadOnlyDictionary<string, long>(this.values);
        Indices = new ReadOnlyDictionary<long, string>(indices);
        SpecialValues = new ReadOnlyDictionary<string, long>(specials);
        SpecialIndices = new ReadOnlyDictionary<long, string>(specials.ToDictionary(p => p.Value, p => p.Key));
    }

    public string GetUnknownValue() => specialTokens.UnkToken;
    public string GetBosValue() => Required(specialTokens.BosToken, "BOS");
    public string GetEosValue() => Required(specialTokens.EosToken, "EOS");
    public string GetPadValue() => Required(specialTokens.PadToken, "PAD");
    public string GetSepValue() => Required(specialTokens.SepToken, "SEP");
    public string GetClsValue() => Required(specialTokens.ClsToken, "CLS");
    public string GetMaskValue() => Required(specialTokens.MaskToken, "MASK");
    private static string Required(string? token, string kind) =>
        token ?? throw new InvalidOperationException("Vocabulary does not define a " + kind + " token");
    public IEnumerable<string> SpecialTokens() => SpecialValues.Keys;
    public long TokenToId(string token) => values.TryGetValue(token, out long id) ? id : values[GetUnknownValue()];
    public string IdToToken(long id) => indices.TryGetValue(id, out string? token) ? token : GetUnknownValue();
    public List<long> ConvertTokensToIds(IEnumerable<string> tokens) => tokens.Select(TokenToId).ToList();

    /// <summary>Ensure that extra_id_0 through extra_id_(numExtraIds-1) exist. Repeated calls are safe.</summary>
    public void AddExtraIds(long numExtraIds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numExtraIds);
        if (numExtraIds > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(numExtraIds));
        AddTokens(Enumerable.Range(0, (int)numExtraIds).Select(i => $"<extra_id_{i}>"));
    }

    /// <summary>Add new tokens in enumeration order without changing existing IDs.</summary>
    public void AddTokens(IEnumerable<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        lock (mutation)
        {
            var additions = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string token in tokens)
            {
                ArgumentException.ThrowIfNullOrEmpty(token);
                if (!values.ContainsKey(token) && seen.Add(token)) additions.Add(token);
            }
            long maximum = values.Values.DefaultIfEmpty(-1).Max();
            _ = checked(maximum + additions.Count);
            foreach (string token in additions)
            {
                long id = ++maximum;
                values.Add(token, id);
                indices.Add(id, token);
            }
        }
    }
}

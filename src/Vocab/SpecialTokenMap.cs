namespace Lokad.Tokenizers.Vocab;

/// <summary>Special-token names. Optional fields are completed by model-specific vocabularies.</summary>
public class SpecialTokenMap
{
    public string UnkToken { get; set; } = "[UNK]";
    public string? PadToken { get; set; }
    public string? BosToken { get; set; }
    public string? SepToken { get; set; }
    public string? ClsToken { get; set; }
    public string? EosToken { get; set; }
    public string? MaskToken { get; set; }
    public HashSet<string> AdditionalSpecialTokens { get; set; } = new(StringComparer.Ordinal);

    internal SpecialTokenMap Copy() => new()
    {
        UnkToken = UnkToken, PadToken = PadToken, BosToken = BosToken, SepToken = SepToken,
        ClsToken = ClsToken, EosToken = EosToken, MaskToken = MaskToken,
        AdditionalSpecialTokens = new(AdditionalSpecialTokens, StringComparer.Ordinal)
    };

    public void RegisterSpecialValues(Dictionary<string, long> values, Dictionary<string, long> specialValues)
    {
        ArgumentException.ThrowIfNullOrEmpty(UnkToken);
        foreach (string? token in new[] { UnkToken, PadToken, BosToken, SepToken, ClsToken, EosToken, MaskToken }
            .Concat(AdditionalSpecialTokens))
            if (token != null) VocabHelper.RegisterAsSpecialValue(token, values, specialValues);
    }
}

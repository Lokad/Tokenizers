namespace Lokad.Tokenizers.Vocab;

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

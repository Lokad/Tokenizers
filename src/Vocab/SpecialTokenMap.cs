namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Represents a map of special tokens used in tokenization.
/// </summary>
public class SpecialTokenMap
{
    private const string DefaultUnkToken = "[UNK]";

    /// <summary>
    /// Gets or sets the unknown token.
    /// </summary>
    public string UnkToken { get; set; }

    /// <summary>
    /// Gets or sets the padding token.
    /// </summary>
    public string PadToken { get; set; }

    /// <summary>
    /// Gets or sets the beginning of sequence token.
    /// </summary>
    public string BosToken { get; set; }

    /// <summary>
    /// Gets or sets the separator token.
    /// </summary>
    public string SepToken { get; set; }

    /// <summary>
    /// Gets or sets the classification token.
    /// </summary>
    public string ClsToken { get; set; }

    /// <summary>
    /// Gets or sets the end of sequence token.
    /// </summary>
    public string EosToken { get; set; }

    /// <summary>
    /// Gets or sets the mask token.
    /// </summary>
    public string MaskToken { get; set; }

    /// <summary>
    /// Gets or sets the additional special tokens.
    /// </summary>
    public HashSet<string> AdditionalSpecialTokens { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecialTokenMap"/> class.
    /// </summary>
    public SpecialTokenMap()
    {
        UnkToken = DefaultUnkToken;
        AdditionalSpecialTokens = new HashSet<string>();
    }

    /// <summary>
    /// Registers special token values in the provided dictionaries.
    /// </summary>
    /// <param name="values">The dictionary of token values and their corresponding IDs.</param>
    /// <param name="specialValues">The dictionary of special token values and their corresponding IDs.</param>
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

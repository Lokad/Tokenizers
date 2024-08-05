namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Represents the vocabulary for XLM-Roberta tokenization.
/// </summary>
public class XlmRobertaVocab : BaseVocab
{
    private const string DefaultUnkToken = "<unk>";
    private const string DefaultPadToken = "<pad>";
    private const string DefaultBosToken = "<s>";
    private const string DefaultSepToken = "</s>";
    private const string DefaultClsToken = "<s>";
    private const string DefaultEosToken = "</s>";
    private const string DefaultMaskToken = "<mask>";

    /// <summary>
    /// Initializes a new instance of the <see cref="XlmRobertaVocab"/> class.
    /// </summary>
    /// <param name="values">The dictionary of token values and their corresponding IDs.</param>
    /// <param name="specialTokenMap">The map of special tokens.</param>
    public XlmRobertaVocab(Dictionary<string, long> values, SpecialTokenMap specialTokenMap)
        : base(values, specialTokenMap)
    {
    }

    /// <summary>
    /// Gets the PAD token value.
    /// </summary>
    /// <returns>The PAD token value.</returns>
    public string GetPadValue()
    {
        return SpecialTokenMap.PadToken ?? DefaultPadToken;
    }

    /// <summary>
    /// Gets the BOS (Beginning of Sentence) token value.
    /// </summary>
    /// <returns>The BOS token value.</returns>
    public string GetBosValue()
    {
        return SpecialTokenMap.BosToken ?? DefaultBosToken;
    }

    /// <summary>
    /// Gets the SEP (Separator) token value.
    /// </summary>
    /// <returns>The SEP token value.</returns>
    public string GetSepValue()
    {
        return SpecialTokenMap.SepToken ?? DefaultSepToken;
    }

    /// <summary>
    /// Gets the CLS (Classification) token value.
    /// </summary>
    /// <returns>The CLS token value.</returns>
    public string GetClsValue()
    {
        return SpecialTokenMap.ClsToken ?? DefaultClsToken;
    }

    /// <summary>
    /// Gets the EOS (End of Sentence) token value.
    /// </summary>
    /// <returns>The EOS token value.</returns>
    public string GetEosValue()
    {
        return SpecialTokenMap.EosToken ?? DefaultEosToken;
    }

    /// <summary>
    /// Gets the MASK token value.
    /// </summary>
    /// <returns>The MASK token value.</returns>
    public string GetMaskValue()
    {
        return SpecialTokenMap.MaskToken ?? DefaultMaskToken;
    }

    /// <summary>
    /// Creates an instance of <see cref="XlmRobertaVocab"/> from a protobuf file.
    /// </summary>
    /// <param name="path">The path to the protobuf file.</param>
    /// <returns>An instance of <see cref="XlmRobertaVocab"/>.</returns>
    public static XlmRobertaVocab FromFile(string path)
    {
        var proto = VocabHelper.OpenProtobufFile(path);

        var specialTokenMap = new SpecialTokenMap
        {
            UnkToken = DefaultUnkToken,
            PadToken = DefaultPadToken,
            BosToken = DefaultBosToken,
            SepToken = DefaultSepToken,
            ClsToken = DefaultClsToken,
            EosToken = DefaultEosToken,
            MaskToken = DefaultMaskToken
        };

        var values = new Dictionary<string, long>();
        AddSpecialToken(values, specialTokenMap.ClsToken);
        AddSpecialToken(values, specialTokenMap.PadToken);
        AddSpecialToken(values, specialTokenMap.EosToken);
        AddSpecialToken(values, specialTokenMap.UnkToken);

        foreach (var piece in proto.Pieces.Skip(3))
        {
            values.Add(piece.Piece, values.Count);
        }

        AddSpecialToken(values, specialTokenMap.MaskToken);

        return new XlmRobertaVocab(values, specialTokenMap);
    }

    /// <summary>
    /// Adds a special token to the values dictionary if it is not already present.
    /// </summary>
    /// <param name="values">The dictionary of token values and their corresponding IDs.</param>
    /// <param name="token">The special token to add.</param>
    private static void AddSpecialToken(Dictionary<string, long> values, string token)
    {
        if (!values.ContainsKey(token))
        {
            values.Add(token, values.Count);
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="XlmRobertaVocab"/> from a protobuf file and a special token mapping file.
    /// </summary>
    /// <param name="path">The path to the protobuf file.</param>
    /// <param name="specialTokenMappingPath">The path to the special token mapping file.</param>
    /// <returns>An instance of <see cref="XlmRobertaVocab"/>.</returns>
    public static XlmRobertaVocab FromFileWithSpecialTokenMapping(string path, string specialTokenMappingPath)
    {
        var proto = VocabHelper.OpenProtobufFile(path);
        var specialTokenMap = VocabHelper.ReadSpecialTokenMappingFile(specialTokenMappingPath);

        var values = new Dictionary<string, long>();

        // Add special tokens to values dictionary
        AddSpecialTokenIfPresent(values, specialTokenMap.ClsToken);
        AddSpecialTokenIfPresent(values, specialTokenMap.PadToken);
        AddSpecialTokenIfPresent(values, specialTokenMap.EosToken);
        AddSpecialTokenIfPresent(values, specialTokenMap.UnkToken);

        foreach (var piece in proto.Pieces.Skip(3))
        {
            values.Add(piece.Piece, values.Count);
        }

        AddSpecialTokenIfPresent(values, specialTokenMap.MaskToken);

        return new XlmRobertaVocab(values, specialTokenMap);
    }

    /// <summary>
    /// Adds a special token to the values dictionary if it is present and not already added.
    /// </summary>
    /// <param name="values">The dictionary of token values and their corresponding IDs.</param>
    /// <param name="token">The special token to add.</param>
    private static void AddSpecialTokenIfPresent(Dictionary<string, long> values, string token)
    {
        if (token != null && !values.ContainsKey(token))
        {
            values.Add(token, values.Count);
        }
    }
}

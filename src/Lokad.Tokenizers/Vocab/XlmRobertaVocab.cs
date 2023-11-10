namespace Lokad.Tokenizers.Vocab;

// TODO: ChatGPT port of https://github.com/guillaume-be/rust-tokenizers/blob/main/main/src/vocab/xlm_roberta_vocab.rs

public class XlmRobertaVocab : BaseVocab
{
    private const string DefaultUnkToken = "<unk>";
    private const string DefaultPadToken = "<pad>";
    private const string DefaultBosToken = "<s>";
    private const string DefaultSepToken = "</s>";
    private const string DefaultClsToken = "<s>";
    private const string DefaultEosToken = "</s>";
    private const string DefaultMaskToken = "<mask>";

    public XlmRobertaVocab(Dictionary<string, long> values, SpecialTokenMap specialTokenMap)
        : base(values, specialTokenMap)
    {
    }

    /// <summary>
    /// Gets the PAD token value.
    /// </summary>
    public string GetPadValue()
    {
        return SpecialTokenMap.PadToken ?? DefaultPadToken;
    }

    /// <summary>
    /// Gets the BOS (Beginning of Sentence) token value.
    /// </summary>
    public string GetBosValue()
    {
        return SpecialTokenMap.BosToken ?? DefaultBosToken;
    }

    /// <summary>
    /// Gets the SEP (Separator) token value.
    /// </summary>
    public string GetSepValue()
    {
        return SpecialTokenMap.SepToken ?? DefaultSepToken;
    }

    /// <summary>
    /// Gets the CLS (Classification) token value.
    /// </summary>
    public string GetClsValue()
    {
        return SpecialTokenMap.ClsToken ?? DefaultClsToken;
    }

    /// <summary>
    /// Gets the EOS (End of Sentence) token value.
    /// </summary>
    public string GetEosValue()
    {
        return SpecialTokenMap.EosToken ?? DefaultEosToken;
    }

    /// <summary>
    /// Gets the MASK token value.
    /// </summary>
    public string GetMaskValue()
    {
        return SpecialTokenMap.MaskToken ?? DefaultMaskToken;
    }

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

    private static void AddSpecialToken(Dictionary<string, long> values, string token)
    {
        if (!values.ContainsKey(token))
        {
            values.Add(token, values.Count);
        }
    }

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

    // Helper method to add special token if present
    private static void AddSpecialTokenIfPresent(Dictionary<string, long> values, string token)
    {
        if (token != null && !values.ContainsKey(token))
        {
            values.Add(token, values.Count);
        }
    }


}

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
}

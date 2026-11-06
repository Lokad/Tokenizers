namespace Lokad.Tokenizers.Vocab;

/// <summary>XLM-R vocabulary with the fixed fairseq special-token ID layout.</summary>
public class XlmRobertaVocab : BaseVocab
{
    public XlmRobertaVocab(Dictionary<string, long> values, SpecialTokenMap specialTokenMap)
        : base(values, Complete(specialTokenMap)) { }

    private static SpecialTokenMap Complete(SpecialTokenMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var result = map.Copy();
        result.PadToken ??= "<pad>";
        result.BosToken ??= result.ClsToken ?? "<s>";
        result.ClsToken ??= result.BosToken;
        result.EosToken ??= result.SepToken ?? "</s>";
        result.SepToken ??= result.EosToken;
        result.MaskToken ??= "<mask>";
        if (result.BosToken != result.ClsToken || result.EosToken != result.SepToken)
            throw new NotSupportedException("XLM-R requires BOS/CLS and EOS/SEP to name the same tokens");
        return result;
    }

    public static XlmRobertaVocab FromFile(string path) =>
        FromModel(VocabHelper.OpenProtobufFile(path));

    public static XlmRobertaVocab FromFileWithSpecialTokenMapping(string path, string specialTokenMappingPath) =>
        FromModel(VocabHelper.OpenProtobufFile(path),
            VocabHelper.ReadSpecialTokenMappingFile(specialTokenMappingPath, "<unk>"));

    internal static XlmRobertaVocab FromModel(ModelProto proto, SpecialTokenMap? mapping = null)
    {
        SentencePieceModel.Validate(proto);
        return FromValidatedModel(proto, mapping);
    }

    internal static XlmRobertaVocab FromValidatedModel(ModelProto proto, SpecialTokenMap? mapping = null)
    {
        var map = Complete(mapping ?? new SpecialTokenMap { UnkToken = "<unk>" });
        var values = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            [map.ClsToken!] = 0, [map.PadToken!] = 1, [map.EosToken!] = 2, [map.UnkToken] = 3
        };
        if (values.Count != 4) throw new InvalidDataException("XLM-R special tokens must be distinct");
        for (int i = 3; i < proto.Pieces.Count; i++)
            if (!values.TryAdd(proto.Pieces[i].Piece, i + 1L))
                throw new InvalidDataException("Model pieces conflict with the special-token mapping");
        if (!values.TryAdd(map.MaskToken!, proto.Pieces.Count + 1L))
            throw new InvalidDataException("Model pieces conflict with the mask token");
        return new(values, map);
    }
}

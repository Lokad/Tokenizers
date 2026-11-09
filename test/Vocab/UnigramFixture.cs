using Lokad.Tokenizers.Tests.Tokenizer;
using Lokad.Tokenizers.Tokenizer;

namespace Lokad.Tokenizers.Vocab.Tests;

/// <summary>Retains only the supported profile headers and three special entries.</summary>
internal static class UnigramFixture
{
    private static readonly Lazy<ModelProto> template = new(() =>
    {
        var source = VocabHelper.OpenProtobufFile(TestUtils.XlmRobertaFixture());
        var result = new ModelProto { TrainerSpec = source.TrainerSpec.Clone(), NormalizerSpec = source.NormalizerSpec.Clone() };
        result.Pieces.Add(source.Pieces.Take(3).Select(p => p.Clone()));
        return result;
    });

    internal static ModelProto Model(params (string Text, float Score)[] pieces)
    {
        var result = template.Value.Clone();
        result.Pieces.Add(pieces.Select(p => new ModelProto.Types.SentencePiece { Piece = p.Text, Score = p.Score }));
        result.TrainerSpec.VocabSize = result.Pieces.Count;
        return result;
    }

    internal static XLMRobertaTokenizer Tokenizer(params (string Text, float Score)[] pieces)
    {
        var model = Model(pieces);
        return new(XlmRobertaVocab.FromModel(model), SentencePieceModel.FromModel(model), false);
    }
}

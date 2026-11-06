using Google.Protobuf;
using System.Text;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Tests.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Vocab.Tests;

public sealed class SentencePieceModelTests
{
    [Fact]
    public void UnknownRunsPreserveAllOffsetsAndMasks()
    {
        var model = new SentencePieceModel();
        var tokens = model.ParseNodesToTokens(new() {
            new Node("🧪", 0, 0, 0, 1, new uint[] { 7 }),
            new Node("𩸽", 0, 0, 1, 2, new uint[] { 8 }),
            new Node("▁a", 0, 3, 2, 4, new uint[] { 9,10 }),
            new Node("🧪", 0, 0, 4, 5, new uint[] { 11 })
        });
        Assert.Equal(new[] { "🧪𩸽", "▁a", "🧪" }, tokens.Select(t => t.Text));
        Assert.Equal(new uint[] { 7,8 }, tokens[0].ReferenceOffsets);
        Assert.Equal(new Offset(7,9), tokens[0].Offset);
        Assert.Equal(new[] { Mask.Unknown, Mask.None, Mask.Unknown }, tokens.Select(t => t.Mask));
    }

    [Fact]
    public void SegmentationKeepsNormalizationSourceCoverage()
    {
        var model = new SentencePieceModel();
        model.Root.Children[new Rune('é')] = new TrieNode("é") { End = true, Index = 3, Score = -1 };
        var token = new Token("e\u0301");
        TokenizationUtils.DecomposeNfkc(token);
        var result = model.ParseNodesToTokens(model.DecodeBackward(model.DecodeForwardTokenRef(token).ToArray()));
        Assert.Single(result);
        Assert.Equal(new uint[] { 0,1 }, TokenAlignment.SourcePositions(result[0]));
        Assert.Equal(new Offset(0,2), result[0].Offset);
    }

    [Fact]
    public void FixedTrieAllocationsGrowLinearly()
    {
        var model = new SentencePieceModel();
        model.Root.Children[new Rune('a')] = new TrieNode("a") { End = true, Index = 3, Score = -1 };
        long Measure(int length)
        {
            var token = new Token(new string('a', length));
            model.DecodeForwardTokenRef(token);
            long before = GC.GetAllocatedBytesForCurrentThread();
            var nodes = model.DecodeForwardTokenRef(token);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(length, model.DecodeBackward(nodes.ToArray()).Count);
            return allocated;
        }
        long small = Measure(1000), medium = Measure(2000), large = Measure(4000);
        Assert.True(medium < small * 2.5, $"Unexpected allocation growth: {small} -> {medium}");
        Assert.True(large < medium * 2.5, $"Unexpected allocation growth: {medium} -> {large}");
        Assert.True(large < 8_000_000, $"Unexpected allocation volume: {large}");
    }

    static readonly Lazy<ModelProto> Template = new(() => VocabHelper.OpenProtobufFile(TestUtils.XlmRobertaFixture()));

    static ModelProto Tiny()
    {
        var model = new ModelProto { TrainerSpec = Template.Value.TrainerSpec.Clone(), NormalizerSpec = Template.Value.NormalizerSpec.Clone() };
        model.Pieces.Add(Template.Value.Pieces.Take(3).Select(p => p.Clone()));
        model.Pieces.Add(new ModelProto.Types.SentencePiece { Piece = "a", Score = -1 });
        model.Pieces.Add(new ModelProto.Types.SentencePiece { Piece = "aa", Score = -0.5f });
        model.TrainerSpec.VocabSize = model.Pieces.Count;
        return model;
    }

    [Theory]
    [InlineData("bpe")]
    [InlineData("byte-fallback")]
    [InlineData("special-ids")]
    [InlineData("normalizer")]
    [InlineData("normalizer-map")]
    [InlineData("prefix")]
    [InlineData("whitespace")]
    [InlineData("piece-type")]
    public void UnsupportedModelSettingsFailAtTheFileBoundary(string setting)
    {
        var model = Tiny();
        switch (setting)
        {
            case "bpe": model.TrainerSpec.ModelType = TrainerSpec.Types.ModelType.Bpe; break;
            case "byte-fallback": model.TrainerSpec.ByteFallback = true; break;
            case "special-ids": model.TrainerSpec.UnkId = 1; break;
            case "normalizer": model.NormalizerSpec.Name = "identity"; break;
            case "normalizer-map": model.NormalizerSpec.PrecompiledCharsmap = ByteString.CopyFromUtf8("changed"); break;
            case "prefix": model.NormalizerSpec.AddDummyPrefix = false; break;
            case "whitespace": model.NormalizerSpec.RemoveExtraWhitespaces = false; break;
            case "piece-type": model.Pieces[3].Type = ModelProto.Types.SentencePiece.Types.Type.UserDefined; break;
        }
        WithModel(model, path => {
            Assert.Throws<NotSupportedException>(() => SentencePieceModel.FromFile(path));
            Assert.Throws<NotSupportedException>(() => XlmRobertaVocab.FromFile(path));
            Assert.Throws<NotSupportedException>(() => new XLMRobertaTokenizer(path, false));
        });
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("score")]
    [InlineData("layout")]
    public void InvalidVocabulariesFailDeterministically(string setting)
    {
        var model = Tiny();
        if (setting == "duplicate") model.Pieces[4].Piece = "a";
        else if (setting == "score") model.Pieces[3].Score = float.NaN;
        else model.Pieces[0].Piece = "changed";
        WithModel(model, path => Assert.Throws<InvalidDataException>(() => new XLMRobertaTokenizer(path, false)));
    }

    static void WithModel(ModelProto model, Action<string> action)
    {
        string path = Path.GetTempFileName();
        try { File.WriteAllBytes(path, model.ToByteArray()); action(path); }
        finally { File.Delete(path); }
    }
}

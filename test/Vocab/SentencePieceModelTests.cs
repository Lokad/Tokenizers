using Google.Protobuf;
using System.Text;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Tests.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Vocab.Tests;

public sealed class SentencePieceModelTests
{
    [Fact]
    public void SmallModelMasksRespectWordsPunctuationAndUnknownBoundaries()
    {
        var model = new SentencePieceModel();
        int id = 3;
        foreach (string piece in new[] { "▁a", "b", "!", "c", "▁d", "e" })
        {
            var node = model.Root;
            foreach (Rune rune in piece.EnumerateRunes())
            {
                if (!node.Children.TryGetValue(rune, out var child)) node.Children.Add(rune, child = new TrieNode(rune.ToString()));
                node = child;
            }
            node.Text = piece; node.End = true; node.Score = -1; node.Index = id++;
        }
        var tokens = model.Tokenize(new Token("▁ab!c▁d🧪e"));
        Assert.Equal(new[] { "▁a", "b", "!", "c", "▁d", "🧪", "e" }, tokens.Select(t => t.Text));
        Assert.Equal(new[] { Mask.None, Mask.Continuation, Mask.Punctuation, Mask.None, Mask.None, Mask.Unknown, Mask.Continuation },
            tokens.Select(t => t.Mask));
        var boundary = new List<Token> { new("<mask>") { Mask = Mask.Special }, new("e"), new(" "), new("e") };
        model.PopulateMasks(boundary, '▁');
        Assert.Equal(new[] { Mask.Special, Mask.Continuation, Mask.Whitespace, Mask.None }, boundary.Select(t => t.Mask));
    }

    [Theory]
    [InlineData(0xD800, "", "")]
    [InlineData(0xDC00, "a", "")]
    [InlineData(0xD800, "a", "b")]
    public void RawSegmentationRejectsMalformedUtf16(int codeUnit, string prefix, string suffix)
    {
        string text = prefix + (char)codeUnit + suffix;
        var model = new SentencePieceModel();
        Assert.Throws<ArgumentException>(() => model.DecodeForwardTokenRef(new Token(text)));
        Assert.Throws<ArgumentException>(() => model.CommonPrefixSearch(text));
    }

    [Fact]
    public void RawSegmentationPreservesValidSupplementaryScalarsAndNoncharacters()
    {
        const string text = "🧪\uFFFE";
        var model = new SentencePieceModel();
        var tokens = model.ParseNodesToTokens(model.DecodeBackward(model.DecodeForwardTokenRef(new Token(text)).ToArray()));
        Assert.Equal(text, Assert.Single(tokens).Text);
        Assert.Equal(new uint[] { 0,1 }, tokens[0].ReferenceOffsets);
    }

    [Theory]
    [InlineData(float.MinValue)]
    [InlineData(float.MaxValue)]
    public void ExtremeFiniteScoresFailAtTheModelBoundary(float score)
    {
        var proto=Tiny();
        proto.Pieces[3].Score=score;
        WithModel(proto,path => Assert.Contains(".score",Assert.Throws<InvalidDataException>(()=>new XLMRobertaTokenizer(path,false)).Message));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(-1e10f)]
    public void SupportedScoresPreserveTextAndOffsetPathAgreement(float score)
    {
        var proto=Tiny();
        proto.Pieces.RemoveAt(4);
        proto.TrainerSpec.VocabSize=4;
        proto.Pieces[3].Score=score;
        WithModel(proto,path => {
            var tokenizer=new XLMRobertaTokenizer(path,false);
            Assert.Equal("▁aaa",string.Concat(tokenizer.Tokenize("aaa")));
            Assert.Equal(tokenizer.Tokenize("aaa"),tokenizer.TokenizeWithOffsets("aaa").Tokens);
        });
    }

    [Fact]
    public void CallerSuppliedTrieCannotMaterializeAnUnsupportedScore()
    {
        var model=new SentencePieceModel();
        model.Root.Children[new Rune('a')]=new TrieNode("a"){End=true,Index=3,Score=float.MinValue};
        Assert.Contains("score",Assert.Throws<InvalidDataException>(()=>model.DecodeForwardTokenRef(new Token("a"))).Message);
    }
    [Fact]
    public void BackwardDecoderRejectsCyclesAndInconsistentEndpoints()
    {
        var model = new SentencePieceModel();
        var cycle = new Node("a",0,3,0,1,new uint[]{0});
        Assert.Throws<ArgumentException>(() => model.DecodeBackward(new Node?[]{cycle,cycle}));
        Assert.Throws<ArgumentException>(() => model.DecodeBackward(new Node?[]{null,new Node("a",0,3,1,2,new uint[]{0})}));
        Assert.Throws<ArgumentException>(() => model.DecodeBackward(new Node?[]{null,null}));
        Assert.Empty(model.DecodeBackward(new Node?[]{null}));
    }

    [Fact]
    public void LoadedTrieRetainsPublicPrefixTextLengthAndMutableChildren()
    {
        WithModel(Tiny(),path => {
            var model=SentencePieceModel.FromFile(path);
            var a=Assert.Single(model.Root.Children).Value;
            Assert.Equal("a",a.Text);
            Assert.Equal(1,a.Length);
            Assert.Equal("aa",a.Children[new Rune('a')].Text);
            Assert.Equal(2,a.Children[new Rune('a')].Length);
            a.Children[new Rune('b')]=new TrieNode("ab"){End=true,Index=5,Score=-1};
            Assert.Equal(new[]{"a","ab"},model.CommonPrefixSearch("ab").Select(n=>n.Text));
        });
    }

    [Fact]
    public void LongPieceConstructionAllocationsGrowLinearly()
    {
        long Measure(int length)
        {
            var proto=Tiny();
            proto.Pieces.RemoveAt(4);
            proto.Pieces[3].Piece=new string('a',length);
            proto.TrainerSpec.VocabSize=4;
            long allocated=0;
            WithModel(proto,path=>{
                SentencePieceModel.FromFile(path);
                long before=GC.GetAllocatedBytesForCurrentThread();
                var model=SentencePieceModel.FromFile(path);
                allocated=GC.GetAllocatedBytesForCurrentThread()-before;
                Assert.Equal(length,Assert.Single(model.CommonPrefixSearch(new string('a',length))).Length);
            });
            return allocated;
        }
        long small=Measure(1000),large=Measure(4000);
        Assert.True(large<small*4.5,$"Unexpected long-piece construction growth: {small} -> {large}");
        Assert.True(large<8_000_000,$"Unexpected long-piece construction allocation: {large}");
    }

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

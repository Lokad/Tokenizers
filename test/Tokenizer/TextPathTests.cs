using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class TextPathTests
{
    [Fact]
    public void JapanesePunctuationKeepsPrecedenceOverItsCjkBlock()
    {
        var vocabulary = new BaseVocab(new() { ["[UNK]"]=0, ["a"]=1, ["・"]=2, ["゠"]=3, ["あ"]=4, ["東"]=5 }, new());
        var tokenizer = new BaseTokenizer<BaseVocab>(vocabulary, false, false);
        var result = tokenizer.TokenizeWithOffsets("a・゠あ東");
        Assert.Equal(new[] { "a", "・", "゠", "あ", "東" }, result.Tokens);
        Assert.Equal(new[] { Mask.None, Mask.Punctuation, Mask.Punctuation, Mask.CJK, Mask.CJK }, result.Masks);
        Assert.Equal(Enumerable.Range(0, 5).Select(i => new Offset((uint)i, (uint)i + 1)), result.Offsets);
        Assert.Equal(new long[] { 1,2,3,4,5 }, tokenizer.Encode("a・゠あ東", null, 5, TruncationStrategy.LongestFirst, 0).TokenIds);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("  ")]
    public void WhitespaceSpecialsAreConsistentAcrossPublicEntries(string literal)
    {
        var vocabulary=new BaseVocab(new(){["[UNK]"]=0,[literal]=1},new(){MaskToken=literal});
        var tokenizer=new BaseTokenizer<BaseVocab>(vocabulary,false,false);
        Assert.Equal(new[]{literal},tokenizer.Tokenize(literal));
        Assert.Equal(new[]{literal},tokenizer.TokenizeWithOffsets(literal).Tokens);
        Assert.Equal(literal,Assert.Single(tokenizer.TokenizeToTokens(new Token(literal))).Text);
        Assert.Equal(new long[]{1},tokenizer.Encode(literal,null,1,TruncationStrategy.LongestFirst,0).TokenIds);
    }

    [Fact]
    public void XlmRecognizesAWhitespacePadLiteralBeforeSkippingWhitespace()
    {
        var vocabulary=new XlmRobertaVocab(new(){["<s>"]=0,[" "]=1,["</s>"]=2,["<unk>"]=3,["<mask>"]=4},
            new(){UnkToken="<unk>",PadToken=" "});
        var tokenizer=new XLMRobertaTokenizer(vocabulary,new SentencePieceModel(),false);
        Assert.Equal(new[]{" "},tokenizer.Tokenize(" "));
        Assert.Equal(new[]{" "},tokenizer.TokenizeWithOffsets(" ").Tokens);
        Assert.Equal(" ",Assert.Single(tokenizer.TokenizeToTokens(new Token(" "))).Text);
        Assert.Equal(new long[]{0,1,2},tokenizer.Encode(" ",null,3,TruncationStrategy.LongestFirst,0).TokenIds);
    }

    [Fact]
    public void PublicTextEditsRetainOriginalDefaultOffsets()
    {
        var token=new Token("abc");
        token.Text="de";
        Assert.Equal(new uint[]{0,1,2},token.ReferenceOffsets);
        Assert.Equal(new Offset(0,3),token.Offset);
    }

    sealed class Custom : BaseTokenizer<BaseVocab>
    {
        internal Custom() : base(new BaseVocab(new(){["[UNK]"]=0},new()),false,false) { }
        public override List<Token> TokenizeToTokens(Token input)
        {
            Assert.Equal(4,input.ReferenceOffsets.Count);
            Assert.Equal(new Offset(0,4),input.Offset);
            return [new Token("override")];
        }
    }

    [Fact]
    public void TextPathPreservesCustomTokenizerVirtualContracts() => Assert.Equal(new[]{"override"},new Custom().Tokenize("🧪abc"));
}

[Collection("XLM-R")]
public sealed class XlmTextPathTests(XlmTokenizerFixture fixture)
{
    [Theory]
    [InlineData("")]
    [InlineData("  \t")]
    [InlineData("Hello world! é<mask>🧪")]
    [InlineData("ﬁe\u0301 a\u0301\u0327 가 İ𩸽\0")]
    public void TextAndOffsetPathsProduceIdenticalPieces(string text)
    {
        foreach(var tokenizer in new[]{fixture.Original,fixture.Lowercase})
            Assert.Equal(tokenizer.TokenizeWithOffsets(text).Tokens,tokenizer.Tokenize(text));
    }

    [Fact]
    public void TextOnlyPathAvoidsOffsetAllocation()
    {
        string text=string.Concat(Enumerable.Repeat("hello world ",333));
        fixture.Original.Tokenize(text);fixture.Original.TokenizeWithOffsets(text);
        long before=GC.GetAllocatedBytesForCurrentThread();
        var pieces=fixture.Original.Tokenize(text);
        long textAllocation=GC.GetAllocatedBytesForCurrentThread()-before;
        before=GC.GetAllocatedBytesForCurrentThread();
        var offsets=fixture.Original.TokenizeWithOffsets(text);
        long offsetAllocation=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(offsets.Tokens,pieces);
        Assert.True(textAllocation<offsetAllocation*0.75,$"Text: {textAllocation}, offsets: {offsetAllocation}");
    }

    [Fact]
    public void CasingWrappersShareVocabularyAndKeepIndependentSettings()
    {
        var lower=fixture.Original.WithLowerCase(true);
        Assert.Same(fixture.Original.Vocab,lower.Vocab);
        Assert.Equal(fixture.Original.Tokenize("hello"),lower.Tokenize("HELLO"));
        Assert.NotEqual(fixture.Original.Tokenize("HELLO"),lower.Tokenize("HELLO"));
    }

    [Fact]
    public void SharedModelSupportsConcurrentCasingConfigurations()
    {
        var lowercase=fixture.Original.WithLowerCase(true);
        const string text="HELLO e\u0301 🧪東京";
        var expected=fixture.Original.Encode(text,null,128,TruncationStrategy.LongestFirst,0).TokenIds;
        var lowerExpected=lowercase.Encode(text,null,128,TruncationStrategy.LongestFirst,0).TokenIds;
        Parallel.For(0,16,i=>{
            var tokenizer=i%2==0?fixture.Original:lowercase;
            Assert.Equal(i%2==0?expected:lowerExpected,tokenizer.Encode(text,null,128,TruncationStrategy.LongestFirst,0).TokenIds);
        });
    }
}

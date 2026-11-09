using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class ExtensionContractTests
{
    private sealed class Conditional() : BaseTokenizer<BaseVocab>(new BaseVocab(new() { ["[UNK]"]=0, ["one"]=1 }, new()), false, false)
    {
        public override TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets first, TokenIdsWithOffsets? second = null)
        {
            var result = base.BuildInputWithSpecialTokens(first,second);
            if (first.Ids.Count > 0) Marker(result);
            return result;
        }
    }

    private static void Marker(TokenIdsWithSpecialTokens result)
    {
        result.TokenIds.Add(9); result.SegmentIds.Add(0); result.SpecialTokensMask.Add(1);
        result.TokenOffsets.Add(null); result.ReferenceOffsets.Add([]); result.Mask.Add(Mask.Special);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ConditionalTemplateCountViolationsAreExplicit(int maximum)
    {
        var tokenizer = new Conditional();
        Assert.Contains("content-independent", Assert.Throws<InvalidOperationException>(() =>
            tokenizer.Encode("one",null,maximum,TruncationStrategy.LongestFirst,0)).Message);
        Assert.Empty(tokenizer.Encode("",null,maximum,TruncationStrategy.LongestFirst,0).TokenIds);
    }

    private class Marked(XlmRobertaVocab vocabulary, SentencePieceModel model, bool lowercase)
        : XLMRobertaTokenizer(vocabulary, model, lowercase)
    {
        public override TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets first, TokenIdsWithOffsets? second = null)
        {
            var result = base.BuildInputWithSpecialTokens(first,second);
            Marker(result);
            return result;
        }
    }

    private sealed class Copying : Marked
    {
        private readonly SentencePieceModel sharedModel;
        internal Copying(XlmRobertaVocab vocabulary, SentencePieceModel model, bool lowercase) : base(vocabulary,model,lowercase) => sharedModel=model;
        public override XLMRobertaTokenizer WithLowerCase(bool value) => new Copying(Vocab, sharedModel, value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CasingFactoryPreservesDerivedBehaviorOrFailsExplicitly(bool lowercase)
    {
        var vocabulary = new XlmRobertaVocab(new() { ["<s>"]=0,["<pad>"]=1,["</s>"]=2,["<unk>"]=3,["<mask>"]=4 }, new() { UnkToken="<unk>" });
        var model = new SentencePieceModel();
        Assert.Contains("override WithLowerCase", Assert.Throws<NotSupportedException>(() => new Marked(vocabulary,model,false).WithLowerCase(lowercase)).Message);
        var original = new Copying(vocabulary,model,false);
        var copied = original.WithLowerCase(lowercase);
        Assert.IsType<Copying>(copied);
        Assert.Same(original.Vocab, copied.Vocab);
        Assert.Equal(new long[] { 0,3,2,9 }, copied.Encode("ABC",null,4,TruncationStrategy.DoNotTruncate,0).TokenIds);
        Assert.Equal(original.Tokenize(lowercase ? "abc" : "ABC"), copied.Tokenize("ABC"));
    }

    [Fact]
    public void ValueTypeVocabularySharesTheConfiguredMatcherButAlternateCallsRemainDistinct()
    {
        var values = new Dictionary<string,long> { ["[UNK]"]=0,["hello"]=1,["qq"]=2 };
        var mapping = new SpecialTokenMap { AdditionalSpecialTokens=["qq"] };
        for (int i=0;i<1000;i++) { string text=$"<literal-{i}>"; values.Add(text,i+3); mapping.AdditionalSpecialTokens.Add(text); }
        var vocabulary = new BaseVocab(values,mapping);
        var reference = new BaseTokenizer<BaseVocab>(vocabulary,false,false);
        var wrapped = new BaseTokenizer<Wrapped>(new Wrapped(vocabulary),false,false);
        var boxed = new BaseTokenizer<IVocab>(new Wrapped(vocabulary),false,false);
        long Measure(Func<List<string>> action)
        {
            action();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i=0;i<3;i++) Assert.Equal(new[] { "hello" },action());
            return (GC.GetAllocatedBytesForCurrentThread()-before)/3;
        }
        long classBytes=Measure(()=>reference.Tokenize("hello"));
        Wrapped.SpecialReads=0;
        long valueBytes=Measure(()=>wrapped.Tokenize("hello"));
        long boxedBytes=Measure(()=>boxed.Tokenize("hello"));
        Assert.Equal(0,Wrapped.SpecialReads);
        Assert.InRange(valueBytes,0,classBytes+128);
        Assert.InRange(boxedBytes,0,classBytes+128);
        var alternate=new BaseVocab(new() { ["[UNK]"]=0,["hh"]=1 },new() { MaskToken="hh" });
        var parts=new Splitter(new Wrapped(vocabulary)).Against("qqhh",alternate);
        Assert.Equal(new[] { "qq","hh" }, parts.Select(p=>p.Text));
        Assert.Equal(new[] { Mask.None,Mask.Special },parts.Select(p=>p.Mask));
    }

    private sealed class Splitter(Wrapped vocabulary) : BaseTokenizer<Wrapped>(vocabulary,false,false)
    { internal List<Token> Against(string text,IVocab other)=>SplitOnSpecialTokens(new Token(text),other); }

    private readonly struct Wrapped(BaseVocab inner) : IVocab
    {
        internal static int SpecialReads;
        public IReadOnlyDictionary<string,long> Values=>inner.Values;
        public IReadOnlyDictionary<long,string> Indices=>inner.Indices;
        public IReadOnlyDictionary<string,long> SpecialValues { get { SpecialReads++; return inner.SpecialValues; } }
        public IReadOnlyDictionary<long,string> SpecialIndices=>inner.SpecialIndices;
        public IEnumerable<string> SpecialTokens()=>inner.SpecialTokens();
        public string GetUnknownValue()=>inner.GetUnknownValue();
        public string GetBosValue()=>inner.GetBosValue();
        public string GetClsValue()=>inner.GetClsValue();
        public string GetEosValue()=>inner.GetEosValue();
        public string GetMaskValue()=>inner.GetMaskValue();
        public string GetPadValue()=>inner.GetPadValue();
        public string GetSepValue()=>inner.GetSepValue();
        public long TokenToId(string text)=>inner.TokenToId(text);
        public string IdToToken(long id)=>inner.IdToToken(id);
        public List<long> ConvertTokensToIds(IEnumerable<string> tokens)=>inner.ConvertTokensToIds(tokens);
        public void AddExtraIds(long count)=>inner.AddExtraIds(count);
        public void AddTokens(IEnumerable<string> tokens)=>inner.AddTokens(tokens);
    }
}

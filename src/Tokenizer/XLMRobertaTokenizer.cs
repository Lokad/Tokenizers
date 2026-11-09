using Lokad.Tokenizers.Vocab;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>XLM-R encoding with literal special tokens, cleaning, NFKC and optional invariant lowercase.</summary>
/// <remarks>Loads the qualified nmt_nfkc unigram model profile. Repeated whitespace is preserved as in the legacy API.</remarks>
public class XLMRobertaTokenizer : BaseTokenizer<XlmRobertaVocab>
{
    private readonly SentencePieceModel model;
    private readonly bool lowerCase;

    /// <summary>Read vocabulary and scores from one local SentencePiece protobuf file.</summary>
    public XLMRobertaTokenizer(string path, bool lowerCase) : this(Load(path, null), lowerCase) { }

    /// <summary>Read one protobuf and a local special-token mapping JSON file.</summary>
    public XLMRobertaTokenizer(string path, bool lowerCase, string specialTokenMappingPath)
        : this(Load(path, specialTokenMappingPath), lowerCase) { }

    private XLMRobertaTokenizer((XlmRobertaVocab Vocabulary, SentencePieceModel Model) loaded, bool lowerCase)
        : this(loaded.Vocabulary, loaded.Model, lowerCase) { }

    private static (XlmRobertaVocab, SentencePieceModel) Load(string path, string? mapping)
    {
        ModelProto proto = VocabHelper.OpenProtobufFile(path);
        SentencePieceModel.Validate(proto);
        return (XlmRobertaVocab.FromValidatedModel(proto,
            mapping == null ? null : VocabHelper.ReadSpecialTokenMappingFile(mapping, "<unk>")),
            SentencePieceModel.FromValidatedModel(proto));
    }

    /// <summary>Use a caller-supplied compatible vocabulary and model. Do not modify either during encoding.</summary>
    public XLMRobertaTokenizer(XlmRobertaVocab vocab, SentencePieceModel model, bool lowerCase)
        : base(vocab, false, false)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        this.lowerCase = lowerCase;
    }

    /// <summary>Create an encoder with the requested casing, sharing this vocabulary and segmentation model.</summary>
    /// <remarks>Finish vocabulary additions before encoding concurrently through either instance. Derived classes must override this factory to preserve their additional behavior.</remarks>
    public virtual XLMRobertaTokenizer WithLowerCase(bool lowerCase)
    {
        if (GetType() != typeof(XLMRobertaTokenizer))
            throw new NotSupportedException("Derived tokenizers must override WithLowerCase to preserve their behavior");
        return new(Vocab, model, lowerCase);
    }

    protected override bool IsEmptyInput(string text) => base.IsEmptyInput(text) ||
        TokenizationUtils.IsEmptyAfterCleaning(text) && !ContainsSpecialToken(text);

    public override List<string> Tokenize(string text)
    {
        if (GetType() != typeof(XLMRobertaTokenizer)) return base.Tokenize(text);
        if (IsEmptyInput(text)) return [];
        return TokenizeToTokens(new Token(text, false)).Select(t => t.Text).ToList();
    }

    public override List<Token> TokenizeToTokens(Token tokenRef)
    {
        ArgumentNullException.ThrowIfNull(tokenRef);
        if (tokenRef.Mask is Mask.Special or Mask.Unknown) UnicodeProfile.ValidateUtf16(tokenRef.Text);
        if (tokenRef.Mask is not Mask.Special and not Mask.Unknown && IsEmptyInput(tokenRef.Text)) return [];
        var result = new List<Token>();
        foreach (Token token in SplitOnSpecialTokens(tokenRef))
        {
            if (token.Mask is Mask.Special or Mask.Unknown) { result.Add(token); continue; }
            TokenizationUtils.CleanText(token);
            TokenizationUtils.DecomposeNfkc(token);
            if (lowerCase) TokenizationUtils.Lowercase(token);
            if (token.Text.Length == 0) continue;
            TokenizationUtils.ReplaceWhitespace(token);
            if (!token.Text.StartsWith(TokenizationUtils.Metaspace))
            {
                if (token.TracksOffsets)
                {
                    uint[][] references = TokenAlignment.References(token);
                    TokenAlignment.Set(token, TokenizationUtils.Metaspace + token.Text,
                        new[] { new[] { references[0].Min() } }.Concat(references).ToArray());
                }
                else token.ReplaceText(TokenizationUtils.Metaspace + token.Text);
            }
            result.AddRange(model.Tokenize(token));
        }
        return result;
    }

    /// <summary>Apply the template to truncated content, copying mutable offsets and source-position lists.</summary>
    public override TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets tokens1,
        TokenIdsWithOffsets? tokens2 = null) => BuildInputCore(tokens1, tokens2, takeOwnership: false);

    internal override int? GetBuiltInSpecialCount(bool paired) => GetType() == typeof(XLMRobertaTokenizer) ? (paired ? 4 : 2) : null;

    internal override TokenIdsWithSpecialTokens BuildInputCore(TokenIdsWithOffsets tokens1,
        TokenIdsWithOffsets? tokens2, bool takeOwnership)
    {
        var result = new TokenIdsWithSpecialTokens();
        void Special(string text, byte segment)
        {
            result.TokenIds.Add(Vocab.TokenToId(text));
            result.SegmentIds.Add(segment);
            result.SpecialTokensMask.Add(1);
            result.TokenOffsets.Add(null);
            result.ReferenceOffsets.Add([]);
            result.Mask.Add(Mask.Special);
        }
        ArgumentNullException.ThrowIfNull(tokens1);
        tokens1.Validate();
        tokens2?.Validate();
        Special(Vocab.GetClsValue(), 0);
        result.Append(tokens1, 0, takeOwnership);
        Special(Vocab.GetSepValue(), 0);
        if (tokens2 != null)
        {
            Special(Vocab.GetSepValue(), 1);
            result.Append(tokens2, 1, takeOwnership);
            Special(Vocab.GetSepValue(), 1);
        }
        return result;
    }

    /// <summary>Join pieces, replace SentencePiece whitespace markers, and remove boundary whitespace.</summary>
    public override string ConvertTokensToString(List<string> tokens) =>
        UnicodeProfile.TrimWhiteSpace(string.Concat(tokens).Replace(TokenizationUtils.Metaspace, ' '));
}

using System.Text;
using Lokad.Tokenizers.Vocab;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>XLM-R encoding with literal special tokens, cleaning, NFKC and optional invariant lowercase.</summary>
/// <remarks>Loads the qualified nmt_nfkc unigram model profile. Repeated whitespace is preserved as in the legacy API.</remarks>
public class XLMRobertaTokenizer : BaseTokenizer<XlmRobertaVocab>
{
    private readonly SentencePieceModel model;
    private readonly bool lowerCase;
    internal SentencePieceModel Model => model;

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

    public override List<Token> TokenizeToTokens(Token tokenRef)
    {
        var result = new List<Token>();
        foreach (Token token in SplitOnSpecialTokens(tokenRef, Vocab))
        {
            if (token.Mask is Mask.Special or Mask.Unknown) { result.Add(token); continue; }
            TokenizationUtils.CleanText(token, true);
            TokenizationUtils.DecomposeNfkc(token);
            if (lowerCase) TokenizationUtils.Lowercase(token);
            if (token.Text.Length == 0) continue;
            var replaced = new StringBuilder(token.Text.Length);
            foreach (Rune scalar in token.Text.EnumerateRunes())
                replaced.Append(TokenizationUtils.IsWhitespace(scalar) ? new Rune(TokenizationUtils.Metaspace) : scalar);
            token.Text = replaced.ToString();
            if (!token.Text.StartsWith(TokenizationUtils.Metaspace))
            {
                uint[][] references = TokenAlignment.References(token);
                TokenAlignment.Set(token, TokenizationUtils.Metaspace + token.Text,
                    new[] { new[] { references[0].Min() } }.Concat(references).ToArray());
            }
            result.AddRange(model.ParseNodesToTokens(model.DecodeBackward(model.DecodeForwardTokenRef(token).ToArray())));
        }
        return result;
    }

    public override TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets tokens1,
        TokenIdsWithOffsets? tokens2 = null)
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
        void Sequence(TokenIdsWithOffsets tokens, byte segment)
        {
            result.TokenIds.AddRange(tokens.Ids);
            result.SegmentIds.AddRange(Enumerable.Repeat(segment, tokens.Ids.Count));
            result.SpecialTokensMask.AddRange(Enumerable.Repeat((byte)0, tokens.Ids.Count));
            result.TokenOffsets.AddRange(tokens.Offsets);
            result.ReferenceOffsets.AddRange(tokens.ReferenceOffsets);
            result.Mask.AddRange(tokens.Masks);
        }
        Special(Vocab.GetClsValue(), 0);
        Sequence(tokens1, 0);
        Special(Vocab.GetSepValue(), 0);
        if (tokens2 != null)
        {
            Special(Vocab.GetSepValue(), 1);
            Sequence(tokens2, 1);
            Special(Vocab.GetSepValue(), 1);
        }
        return result;
    }

    /// <summary>Join pieces, replace SentencePiece whitespace markers, and remove boundary whitespace.</summary>
    public override string ConvertTokensToString(List<string> tokens) =>
        string.Concat(tokens).Replace(TokenizationUtils.Metaspace, ' ').Trim();
}

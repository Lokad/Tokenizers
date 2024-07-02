using Lokad.Tokenizers.Vocab;
using System.Text;

// TODO: Port of ChatGPT from https://github.com/guillaume-be/rust-tokenizers/blob/main/main/src/tokenizer/xlm_roberta_tokenizer.rs

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// XLM RoBERTa tokenizer performing:
/// - Splitting on special tokens
/// - text cleaning
/// - NFKC decomposition
/// - (optional) lower casing
/// - SentencePiece decomposition
/// </summary>
public class XLMRobertaTokenizer : BaseTokenizer<XlmRobertaVocab>
{
    private SentencePieceModel _model;
    private XlmRobertaVocab _vocab;
    private bool _lowerCase;

    /// <summary>
    /// Create a new instance of a `XLMRobertaTokenizer`
    /// Expects a json vocab file and a SentencePiece protobuf file as an input.
    /// </summary>
    public XLMRobertaTokenizer(string path, bool lowerCase)
        : base(XlmRobertaVocab.FromFile(path), lowerCase, false)
    {
        _model = SentencePieceModel.FromFile(path);
        _vocab = Vocab;
        _lowerCase = lowerCase;
    }

    /// <summary>
    /// Create a new instance of a `XLMRobertaTokenizer` with special token mapping.
    /// Expects a json vocab file, a SentencePiece protobuf file, and a special token mapping file as inputs.
    /// </summary>
    public XLMRobertaTokenizer(string path, bool lowerCase, string specialTokenMappingPath)
        : base(XlmRobertaVocab.FromFileWithSpecialTokenMapping(path, specialTokenMappingPath), lowerCase, false)
    {
        _model = SentencePieceModel.FromFile(path);
        _vocab = Vocab;
        _lowerCase = lowerCase;
    }

    /// <summary>
    /// Create a new instance of a `XLMRobertaTokenizer` from an existing vocabulary and model.
    /// </summary>
    public XLMRobertaTokenizer(XlmRobertaVocab vocab, SentencePieceModel model, bool lowerCase)
        : base(vocab, lowerCase, false)
    {
        _vocab = vocab;
        _model = model;
        _lowerCase = lowerCase;
    }

    /// <summary>
    /// Tokenizes the given text into a list of tokens.
    /// </summary>
    public override List<Token> TokenizeToTokens(Token tokenRef)
    {
        var tokens = SplitOnSpecialTokens(tokenRef, _vocab)
            .Select(t => t.Clone())
            .ToList();

        var subTokens = new List<Token>();
        foreach (var token in tokens)
        {
            if (token.Mask != Mask.Special && token.Mask != Mask.Unknown)
            {
                TokenizationUtils.CleanText(token, true);
                TokenizationUtils.DecomposeNfkc(token);
                if (_lowerCase)
                {
                    TokenizationUtils.Lowercase(token);
                }

                // Manually replacing whitespace characters
                var newText = new StringBuilder();
                foreach (var c in token.Text.EnumerateRunes())
                {
                    newText.Append(TokenizationUtils.IsWhitespace(c) ? new Rune('\u2581') : c.ToString());
                }
                token.Text = newText.ToString();

                if (!token.Text.StartsWith('\u2581'))
                {
                    token.Text = "\u2581" + token.Text;
                    var newReferenceOffsets = new List<uint> { 0 };
                    newReferenceOffsets.AddRange(token.ReferenceOffsets);
                    token.ReferenceOffsets = newReferenceOffsets;
                }

                var output = _model.DecodeForwardTokenRef(token);
                var decoded = _model.DecodeBackward(output.ToArray());
                var outputTokens = _model.ParseNodesToTokens(decoded);
                subTokens.AddRange(outputTokens);
            }
            else
            {
                subTokens.Add(token.Clone());
            }
        }
        return subTokens;
    }

    /// <summary>
    /// Converts a list of tokens to a single string.
    /// </summary>
    public string ConvertTokensToString(List<string> tokens)
    {
        return string.Join("", tokens.Select(t => t.Replace("\u2581", " ")));
    }

    /// <summary>
    /// Builds input with special tokens.
    /// </summary>
    public TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets tokens1, TokenIdsWithOffsets tokens2 = null)
    {
        var output = new List<long> { _vocab.TokenToId(_vocab.GetClsValue()) };
        var tokenSegmentIds = new List<int> { 0 };
        var specialTokensMask = new List<int> { 1 };
        var offsets = new List<Offset?> { null };
        var originalOffsets = new List<List<uint>> { new List<uint>() };
        var mask = new List<Mask> { Mask.Special };

        output.AddRange(tokens1.Ids);
        tokenSegmentIds.AddRange(Enumerable.Repeat(0, tokens1.Ids.Count));
        specialTokensMask.AddRange(Enumerable.Repeat(0, tokens1.Ids.Count));
        offsets.AddRange(tokens1.Offsets);
        originalOffsets.AddRange(tokens1.ReferenceOffsets);
        mask.AddRange(tokens1.Masks);

        output.Add(_vocab.TokenToId(_vocab.GetSepValue()));
        tokenSegmentIds.Add(0);
        specialTokensMask.Add(1);
        offsets.Add(null);
        originalOffsets.Add(new List<uint>());
        mask.Add(Mask.Special);

        if (tokens2 != null)
        {
            output.Add(_vocab.TokenToId(_vocab.GetSepValue()));
            output.AddRange(tokens2.Ids);
            tokenSegmentIds.AddRange(Enumerable.Repeat(1, tokens2.Ids.Count + 1));
            specialTokensMask.AddRange(Enumerable.Repeat(0, tokens2.Ids.Count).Prepend(1));
            offsets.AddRange(tokens2.Offsets.Prepend(null));
            originalOffsets.AddRange(tokens2.ReferenceOffsets.Prepend(new List<uint>()));
            mask.AddRange(tokens2.Masks.Prepend(Mask.Special));

            output.Add(_vocab.TokenToId(_vocab.GetSepValue()));
            tokenSegmentIds.Add(1);
            specialTokensMask.Add(1);
            offsets.Add(null);
            originalOffsets.Add(new List<uint>());
            mask.Add(Mask.Special);
        }

        return new TokenIdsWithSpecialTokens
        {
            TokenIds = output,
            SegmentIds = tokenSegmentIds.Select(i => (byte)i).ToList(),
            SpecialTokensMask = specialTokensMask.Select(i => (byte)i).ToList(),
            TokenOffsets = offsets,
            ReferenceOffsets = originalOffsets,
            Mask = mask
        };
    }
}
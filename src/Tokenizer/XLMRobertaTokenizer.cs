using Lokad.Tokenizers.Vocab;
using System.Text;

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
    private readonly SentencePieceModel _model;
    private readonly XlmRobertaVocab _vocab;
    private readonly bool _lowerCase;

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
                    newText.Append(TokenizationUtils.IsWhitespace(c) ? new Rune(Constants.LowerOneEighthBlock) : c.ToString());
                }
                token.Text = newText.ToString();

                if (!token.Text.StartsWith(Constants.LowerOneEighthBlock))
                {
                    token.Text = Constants.LowerOneEighthBlock + token.Text;
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
        return string.Join("", tokens.Select(t => t.Replace(Constants.LowerOneEighthBlock.ToString(), " ")));
    }


}
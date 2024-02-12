using System.Dynamic;
using System.Text;
using Lokad.Tokenizers.Vocab;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;

// TODO: ChatGPT port of https://github.com/guillaume-be/rust-tokenizers/blob/main/main/src/tokenizer/tokenization_utils.rs
// TODO: unit tests not ported

// Port notes:
// - OffsetSize is ported as 'uint'
// - Token and TokenRef have been merged as 'Token'.

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Truncation strategy variants
/// Indicates if and how sequence pairs exceeding a given length should be truncated
/// </summary>
public enum TruncationStrategy
{
    /// <summary>
    /// Truncate the longest sequence first
    /// </summary>
    LongestFirst,

    /// <summary>
    /// Truncate only the first sequence
    /// </summary>
    OnlyFirst,

    /// <summary>
    /// Truncate only the second sequence
    /// </summary>
    OnlySecond,

    /// <summary>
    /// Do not truncate the sequences
    /// </summary>
    DoNotTruncate,
}


/// <summary>
/// Offset information (in unicode points) to relate a token back to its original input string
/// </summary>
public class Offset : IEquatable<Offset>
{
    public uint Begin { get; set; }
    public uint End { get; set; }

    /// <summary>
    /// Create a new offset from a begin and end positions
    /// </summary>
    public Offset(uint begin, uint end)
    {
        Begin = begin;
        End = end;
    }

    /// <summary>
    /// Wrap the offset into an option
    /// </summary>
    public Offset? IntoOption()
    {
        if (End > Begin)
        {
            return this;
        }
        else
        {
            return null;
        }
    }

    public override String ToString() => $"({Begin}:{End})";

    public bool Equals(Offset? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Begin == other.Begin && End == other.End;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((Offset)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Begin * 397) ^ (int)End;
        }
    }

    public static bool operator ==(Offset? left, Offset? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Offset? left, Offset? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Type indication for tokens (e.g. special token, white space, unknown...)
/// </summary>
public enum Mask
{
    /// <summary>
    /// The token has no particular mask. This is the default situation. It may indicate that further processing can be done on a token.
    /// </summary>
    None,

    /// <summary>
    /// The token represents a whitespace (in any shape or form)
    /// </summary>
    Whitespace,

    /// <summary>
    /// The token represents punctuation (in any shape or form)
    /// </summary>
    Punctuation,

    /// <summary>
    /// The token represents a single Chinese/Japanese/Korean character (including kana and hangul)
    /// </summary>
    CJK,

    /// <summary>
    /// The token is a special marker (such as a separator marker, a class marker, etc)
    /// </summary>
    Special,

    /// <summary>
    /// The token is the begin in a series of subtokens, the offset refers specifically to the sub-token. Subsequent tokens in this sequence will carry the 'Continuation' mask
    /// </summary>
    Begin,

    /// <summary>
    /// The token is the continuation of the previous token, the offset refers specifically to the sub-token. All but the first sub-token in a sequence carry this mask (the first carries 'Begin'). (this is the reverse of Mask::Unfinished)
    /// </summary>
    Continuation,

    /// <summary>
    /// The token is the start of a token but not finished yet. All but the last sub-token in the a token sequence carry this mask. This is the reverse of Mask::Continuation.
    /// </summary>
    Unfinished,

    /// <summary>
    /// The token is out of vocabulary, it is unknown by the tokenizer and it will decode to unknown. Tokens that can be decoded properly (but may still be out of vocabulary) should not set this.
    /// </summary>
    Unknown,
}

/// <summary>
/// Token abstraction trait to access token fields, irrespective of their form (reference of owned)
/// </summary>
public interface ITokenTrait
{
    /// <summary>
    /// Returns the offset of the token with respect to the original string
    /// </summary>
    Offset Offset { get; }

    /// <summary>
    /// Returns the token mask
    /// </summary>
    Mask Mask { get; }

    /// <summary>
    /// Returns a string representation for the token
    /// </summary>
    string AsStr();
}

/// <summary>
/// Owned token that references the original text but stores its own string representation.
/// </summary>
public class Token : ITokenTrait
{
    /// <summary>
    /// String representation
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Start and end positions of the token with respect to the original text
    /// </summary>
    public Offset Offset { get; set; }

    /// <summary>
    /// Sequence of positions with respect to the original text contained in the token.
    /// For example, if the token offset is `start: 4, end: 10`, corresponding reference_offsets are `[4, 5, 6, 7, 8, 9]`
    /// </summary>
    public IReadOnlyList<uint> ReferenceOffsets { get; set; }

    /// <summary>
    /// Mask indicating the type of the token
    /// </summary>
    public Mask Mask { get; set; }

    /// <summary>
    /// Creates a new owned token from a `String`.
    /// </summary>
    /// <param name="text">text reference</param>
    public Token(string text)
    {
        Text = text;
        uint text_size = (uint)text.Length;
        Offset = new Offset(0, text_size);
        ReferenceOffsets = Enumerable.Range(0, (int)text_size).Select(i => (uint)i).ToList();
        Mask = Mask.None;
    }

    /// <summary>
    /// Creates a new token from a text and list of offsets.
    /// </summary>
    /// <param name="text">text reference</param>
    /// <param name="offsets">reference positions with respect to the original text</param>
    public Token(string text, uint[] offsets)
    {
        Text = text;
        Offset = new Offset(0, (uint)offsets.Length);
        ReferenceOffsets = offsets;
        Mask = Mask.None;
    }

    public Token(string text, Offset offset, IReadOnlyList<uint> referenceOffsets, Mask mask)
    {
        Text = text;
        Offset = offset;
        ReferenceOffsets = referenceOffsets;
        Mask = mask;
    }

    public string AsStr()
    {
        return Text;
    }

    public static Token From(string text)
    {
        return new Token(text);
    }

    public Token Clone()
    {
        return new Token(Text, Offset, new List<uint>(ReferenceOffsets), Mask);
    }
}


/// <summary>
/// Tokenized Input, ready for processing in language models
/// This represents the final output of the encoding process (tokenized sentence with encoded values)
/// </summary>
public class TokenizedInput
{
    /// <summary>
    /// Vector of token IDs
    /// </summary>
    public List<long> TokenIds { get; set; }

    /// <summary>
    /// Vector segments ids (for example for BERT segments are separated with a [SEP] marker, each incrementing the segment ID).
    /// This vector has the same length as token_ids.
    /// </summary>
    public List<byte> SegmentIds { get; set; }

    /// <summary>
    /// Flags tokens as special tokens (1) or not (0). This vector has the same length as token_ids.
    /// </summary>
    public List<byte> SpecialTokensMask { get; set; }

    /// <summary>
    /// Vector containing overflowing tokens, populated following a truncation step
    /// </summary>
    public List<long> OverflowingTokens { get; set; }

    /// <summary>
    /// Number of overflowing tokens following a truncation step. this equals the length `overflowing_tokens`
    /// </summary>
    public int NumTruncatedTokens { get; set; }

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<Offset?> TokenOffsets { get; set; }

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<List<uint>> ReferenceOffsets { get; set; }

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This vector has the same length as token_ids.
    /// </summary>
    public List<Mask> Mask { get; set; }
}

/// <summary>
/// Encoded input with special tokens
/// Intermediate tokenization steps before truncation to a maximum length, after encoding and addition of special tokens
/// </summary>
public class TokenIdsWithSpecialTokens
{
    /// <summary>
    /// Vector of token IDs
    /// </summary>
    public List<long> TokenIds { get; set; }

    /// <summary>
    /// Vector segments ids (for example for BERT segments are separated with a [SEP] marker, each incrementing the segment ID).
    /// This vector has the same length as token_ids.
    /// </summary>
    public List<byte> SegmentIds { get; set; }

    /// <summary>
    /// Flags tokens as special tokens (1) or not (0). This vector has the same length as token_ids.
    /// </summary>
    public List<byte> SpecialTokensMask { get; set; }

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<Offset?> TokenOffsets { get; set; }

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<List<uint>> ReferenceOffsets { get; set; }

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This vector has the same length as token_ids.
    /// </summary>
    public List<Mask> Mask { get; set; }
}

/// <summary>
/// Tokenized sequence
/// Intermediate tokenization steps before encoding, addition of special tokens and truncation
/// </summary>
public class TokensWithOffsets
{
    /// <summary>
    /// Vector of token strings
    /// </summary>
    public List<string> Tokens { get; set; }

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<Offset?> Offsets { get; set; }

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<IReadOnlyList<uint>> ReferenceOffsets { get; set; }

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This vector has the same length as token_ids.
    /// </summary>
    public List<Mask> Masks { get; set; }
}

/// <summary>
/// Encoded sequence
/// Intermediate tokenization steps before addition of special tokens, after encoding
/// </summary>
public class TokenIdsWithOffsets
{
    /// <summary>
    /// Vector of token IDs
    /// </summary>
    public List<long> Ids { get; set; }

    /// <summary>
    /// Offset information (as start and end positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<Offset?> Offsets { get; set; }

    /// <summary>
    /// Offset information (as a sequence of positions) in relation to the original text. Tokens that can not be related to the
    /// original source are registered as None.
    /// </summary>
    public List<List<uint>> ReferenceOffsets { get; set; }

    /// <summary>
    /// Masks tokens providing information on the type of tokens. This vector has the same length as token_ids.
    /// </summary>
    public List<Mask> Masks { get; set; }
}

/// <summary>
/// Base trait for tokenizers
/// </summary>
public interface ITokenizer<T> where T : IVocab
{
    /// <summary>
    /// Returns a reference to the tokenizer vocabulary
    /// </summary>
    T Vocab { get; }

    /// <summary>
    /// Tokenize a string, returns a vector of tokens as strings.
    /// Use `TokenizeWithOffsets` or `TokenizeToTokens` to return offset information.
    /// </summary>
    /// <param name="text">text (string-like) to tokenize</param>
    /// <returns>`List<string>` containing the tokens string representation</returns>
    List<string> Tokenize(string text);

    /// <summary>
    /// Tokenize a string, returning tokens with offset information
    /// </summary>
    /// <param name="text">text (string-like) to tokenize</param>
    /// <returns>`TokensWithOffsets` with the tokens and their offset information</returns>
    TokensWithOffsets TokenizeWithOffsets(string text);

    /// <summary>
    /// Tokenize a TokenRef, returning a sequence of tokens
    /// </summary>
    /// <param name="initialToken">TokenRef to tokenize (this is especially useful for nested tokenization, where a tokenizer is called on the ouput of a pre-tokenizer, such as BERT).</param>
    /// <returns>`List<Token>` tokenization of the original `TokenRef`</returns>
    List<Token> TokenizeToTokens(Token initialToken);

    /// <summary>
    /// Convert a slice of string-like to a vector ot token indices
    /// </summary>
    /// <param name="tokens">list of token string-like to convert to ids</param>
    /// <returns>`List<long>` with the token indices</returns>
    List<long> ConvertTokensToIds(List<string> tokens);

    /// <summary>
    /// Converts a sequence of ids (integer) into a string, using the tokenizer and vocabulary
    /// with options to remove special tokens and clean up tokenization spaces.
    /// </summary>
    /// <param name="tokenIds">list of tokenized input ids. Can be obtained using the `Encode` or `EncodePlus` methods.</param>
    /// <param name="skipSpecialTokens">if set to True, will replace special tokens.</param>
    /// <param name="cleanUpTokenizationSpaces">if set to True, will clean up the tokenization spaces.</param>
    /// <returns>`string`: decoded sentence</returns>
    string Decode(List<long> tokenIds, bool skipSpecialTokens, bool cleanUpTokenizationSpaces);

    /// <summary>
    /// Converts a sequence of strings into a single string. This will clean-up artifacts from tokenization
    /// (for example `sub ##word`) and generate a single output string
    /// </summary>
    /// <param name="tokens">list of tokens to concatenate.</param>
    /// <returns>`string`: concatenated sentence string</returns>
    string ConvertTokensToString(List<string> tokens);

    /// <summary>
    /// Cleans-up tokenization artifacts (for example whitespace before punctuation)
    /// </summary>
    /// <param name="inputString">input string to clean up</param>
    /// <returns>`string`: clean-up string</returns>
    string CleanUpTokenization(string inputString);
}

/// <summary>
/// Base tokenizer performing:
/// - whitespace tokenization
/// - splitting on special characters
/// - splitting on punctuation
/// - splitting on CJK characters
/// - (optional) lower casing
/// - (optional) accent stripping
/// </summary>
public class BaseTokenizer<T> where T : IVocab
{
    private T _vocab;
    private bool _lowerCase;
    private bool _stripAccents;

    /// <summary>
    /// Create a new instance of a `BaseTokenizer`
    /// Expects a vocabulary flat-file and special token mapping file as inputs.
    /// </summary>
    /// <param name="path">path to the vocabulary file (only used for special character splitting)</param>
    /// <param name="lowerCase">flag indicating if the text should be lower-cased as part of the tokenization</param>
    /// <param name="stripAccents">flag indicating if accents should be stripped from the text</param>
    /// <param name="specialTokenMappingPath">path to a special token mapping file to overwrite default special tokens</param>
    public BaseTokenizer(string path, bool lowerCase, bool stripAccents, string specialTokenMappingPath)
    {
        _vocab = (T)Activator.CreateInstance(typeof(T), path, specialTokenMappingPath);
        _lowerCase = lowerCase;
        _stripAccents = stripAccents;
    }

    /// <summary>
    /// Create a new instance of a `BaseTokenizer`
    /// Expects a vocabulary flat-file as an input.
    /// </summary>
    /// <param name="path">path to the vocabulary file (only used for special character splitting)</param>
    /// <param name="lowerCase">flag indicating if the text should be lower-cased as part of the tokenization</param>
    /// <param name="stripAccents">flag indicating if accents should be stripped from the text</param>
    public BaseTokenizer(string path, bool lowerCase, bool stripAccents)
    {
        _vocab = (T)Activator.CreateInstance(typeof(T), path);
        _lowerCase = lowerCase;
        _stripAccents = stripAccents;
    }

    /// <summary>
    /// Create a new instance of a `BaseTokenizer` from an existing vocabulary
    /// </summary>
    /// <param name="vocab">Thread-safe reference to a vocabulary</param>
    /// <param name="lowerCase">flag indicating if the text should be lower-cased as part of the tokenization</param>
    /// <param name="stripAccents">flag indicating if accents should be stripped from the text</param>
    public BaseTokenizer(T vocab, bool lowerCase, bool stripAccents)
    {
        _vocab = vocab;
        _lowerCase = lowerCase;
        _stripAccents = stripAccents;
    }

    /// <summary>
    /// Returns a reference to the tokenizer vocabulary
    /// </summary>
    public T Vocab
    {
        get { return _vocab; }
    }

    /// <summary>
    /// Tokenize a string, returns a vector of tokens as strings.
    /// Use `TokenizeWithOffsets` or `TokenizeToTokens` to return offset information.
    /// </summary>
    /// <param name="text">text (string-like) to tokenize</param>
    /// <returns>`List<string>` containing the tokens string representation</returns>
    public List<string> Tokenize(string text)
    {
        return TokenizeWithOffsets(text).Tokens;
    }

    /// <summary>
    /// Tokenize a string, returning tokens with offset information
    /// </summary>
    /// <param name="text">text (string-like) to tokenize</param>
    /// <returns>`TokensWithOffsets` with the tokens and their offset information</returns>
    public TokensWithOffsets TokenizeWithOffsets(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TokensWithOffsets
            {
                Tokens = new List<string>(),
                Offsets = new List<Offset?>(),
                ReferenceOffsets = new List<IReadOnlyList<uint>>(),
                Masks = new List<Mask>(),
            };
        }

        var initialOffsets = Enumerable.Range(0, text.Length).Select(i => (uint)i).ToArray();
        var initialToken = new Token(text, initialOffsets);
        var tokens = TokenizeToTokens(initialToken);
        var length = tokens.Count;
        var texts = new List<string>(length);
        var offsets = new List<Offset?>(length);
        var originalPositions = new List<IReadOnlyList<uint>>(length);
        var masks = new List<Mask>(length);

        foreach (var token in tokens)
        {
            texts.Add(token.Text);
            offsets.Add(token.ReferenceOffsets.Any() ? new Offset(token.ReferenceOffsets.First(), token.ReferenceOffsets.Last() + 1) : (Offset?)null);
            originalPositions.Add(token.ReferenceOffsets);
            masks.Add(token.Mask);
        }

        return new TokensWithOffsets
        {
            Tokens = texts,
            Offsets = offsets,
            ReferenceOffsets = originalPositions,
            Masks = masks,
        };
    }

    /// <summary>
    /// Tokenize a Token, returning a sequence of tokens
    /// </summary>
    /// <param name="initialToken">Token to tokenize (this is especially useful for nested tokenization, where a tokenizer is called on the ouput of a pre-tokenizer, such as BERT).</param>
    /// <returns>`List<Token>` tokenization of the original `Token`</returns>
    public virtual List<Token> TokenizeToTokens(Token initialToken)
    {
        //split on whitespace
        var tokens = WhitespaceTokenize(initialToken)
            .SelectMany(token =>
            {
                //split on special tokens
                return SplitOnSpecialTokens(token, _vocab);
            })
            .SelectMany(token =>
            {
                //split on punctuation (with care for maintaining special values)
                return SplitOnPunct(token);
            })
            .SelectMany(token =>
            {
                //tokenize CJK characters so each character is one token
                return TokenizeCjkChars(token);
            })
            .Select(token =>
            {
                // v-- this is where the token gets owned, all steps above handle Token (dealing with &str)
                var ownedToken = new Token(token.Text)
                {
                    Offset = token.Offset,
                    ReferenceOffsets = token.ReferenceOffsets.ToList(),
                    Mask = token.Mask
                };

                if (ownedToken.Mask != Mask.Special && ownedToken.Mask != Mask.Unknown)
                {
                    CleanText(ownedToken, true);
                    //apply the necessary transformations to the actual tokens (unless it's a special value)
                    if (_lowerCase)
                    {
                        Lowercase(ownedToken);
                    }
                    if (_stripAccents)
                    {
                        StripAccents(ownedToken);
                    }
                }

                return ownedToken;
            })
            .Where(token => !string.IsNullOrEmpty(token.Text))
            .ToList();

        return tokens;
    }

    //public virtual List<Token> TokenizeToTokens(Token initialToken)
    //{
    //    List<Token> tokens = SplitOnSpecialTokens(initialToken, this.Vocab)
    //                         .ToList();

    //    List<Token> subTokens = new List<Token>();
    //    foreach (var token in tokens)
    //    {
    //        if (token.Mask != Mask.Special && token.Mask != Mask.Unknown)
    //        {
    //            CleanText(token, true);
    //            DecomposeNfkc(token);
    //            if (this._lowerCase)
    //            {
    //                Lowercase(token);
    //            }
    //            token.Text = new string(token.Text.Select(c => char.IsWhiteSpace(c) ? '\u2581' : c).ToArray());
    //            if (!token.Text.StartsWith("\u2581"))
    //            {
    //                token.Text = "\u2581" + token.Text;
    //                //token.ReferenceOffsets.Insert(0, 0);
    //            }
    //            var output = model.DecodeForwardTokenRef(token);
    //            var decoded = model.DecodeBackward(output);

    //            var outputTokens = model.ParseNodesToTokens(decoded);
    //            subTokens.AddRange(outputTokens);
    //        }
    //        else
    //        {
    //            subTokens.Add(new Token(token.Text) { Mask = token.Mask, ReferenceOffsets = token.ReferenceOffsets.ToList() });
    //        }
    //    }
    //    return subTokens;
    //}

    public void DecomposeNfkc(Token token)
    {
        // Perform NFKC normalization on the token text
        string decomposedText = token.Text.Normalize(NormalizationForm.FormKC);

        // Calculate the new reference offsets
        List<uint> newReferenceOffsets = new List<uint>();
        uint currentOffset = 0;
        for (int i = 0; i < decomposedText.Length; i++)
        {
            // Assuming the original text was decomposed into single characters
            // Adjust the offset accordingly
            newReferenceOffsets.Add(currentOffset);
            currentOffset += (uint)decomposedText[i].ToString().Length;
        }

        // Update the token's properties
        token.Text = decomposedText;
        token.ReferenceOffsets = newReferenceOffsets;
        token.Offset.Begin = newReferenceOffsets.FirstOrDefault();
        token.Offset.End = newReferenceOffsets.LastOrDefault() + 1;
    }

    /// <summary>
    /// Convert a slice of string-like to a vector ot token indices
    /// </summary>
    /// <param name="tokens">list of token string-like to convert to ids</param>
    /// <returns>`List<long>` with the token indices</returns>
    public List<long> ConvertTokensToIds(List<string> tokens)
    {
        return tokens.Select(token => _vocab.TokenToId(token)).ToList();
    }

    public TokenizedInput Encode(XLMRobertaTokenizer tokenizer, String text1, String? text2, int maxLen, TruncationStrategy truncationStrategy,
        int stride)
    {
        var tokens = TokenizeWithOffsets(text1/*.Normalize()*/);
        var token_ids_1 = ConvertTokensToIds(tokens.Tokens);
        var len_1 = token_ids_1.Count;

        var token_ids_with_offsets_1 = new TokenIdsWithOffsets
        {
            Ids = token_ids_1,
            Offsets = tokens.Offsets,
            ReferenceOffsets = tokens.ReferenceOffsets.Select(_ => _.ToList()).ToList(),
            Masks = tokens.Masks,
        };

        //let (token_ids_with_offsets_2, len_2) = {
        //    if let Some(text) = text_2 {
        //        let tokens_2 = self.tokenize_with_offsets(text);
        //        let token_ids_2: Vec<i64> = self.convert_tokens_to_ids(&tokens_2.tokens);
        //        let len_2 = token_ids_2.len();
        //        (
        //            Some(TokenIdsWithOffsets {
        //            ids: token_ids_2,
        //            offsets: tokens_2.offsets,
        //            reference_offsets: tokens_2.reference_offsets,
        //            masks: tokens_2.masks,
        //        }),
        //        len_2,

        //            )
        //    } else {
        //        (None, 0)
        //    }
        //};

        //let additional_tokens = self.build_input_with_special_tokens(
        //    TokenIdsWithOffsets {
        //    ids: vec![],
        //    offsets: vec![],
        //    reference_offsets: vec![],
        //    masks: vec![],
        //},
        //if token_ids_with_offsets_2.is_some() {
        //    Some(TokenIdsWithOffsets {
        //        ids: vec![],
        //        offsets: vec![],
        //        reference_offsets: vec![],
        //        masks: vec![],
        //    })
        //} else {
        //    None
        //},
        //);

        var total_len = len_1; // + len_2 + additional_tokens.token_ids.len();
        var num_truncated_tokens = total_len > maxLen ? total_len - maxLen : 0;

        var (token_ids_with_offsets_1_x, token_ids_with_offsets_2, overflowing_tokens, _overflowing_offsets) =
            TokenizationUtils.TruncateSequences(token_ids_with_offsets_1, null /*token_ids_with_offsets_2*/,
                num_truncated_tokens, truncationStrategy, stride);
        token_ids_with_offsets_1 = token_ids_with_offsets_1_x;

        var merged_tokenized_input =
            tokenizer.BuildInputWithSpecialTokens(token_ids_with_offsets_1, token_ids_with_offsets_2);

        return new TokenizedInput()
        {
            TokenIds = merged_tokenized_input.TokenIds,
            SegmentIds = merged_tokenized_input.SegmentIds,
            SpecialTokensMask = merged_tokenized_input.SpecialTokensMask,
            OverflowingTokens = overflowing_tokens,
            NumTruncatedTokens = num_truncated_tokens,
            TokenOffsets = merged_tokenized_input.TokenOffsets,
            ReferenceOffsets = merged_tokenized_input.ReferenceOffsets,
            Mask = merged_tokenized_input.Mask
        };
    }

    /// <summary>
    /// Converts a sequence of ids (integer) into a string, using the tokenizer and vocabulary
    /// with options to remove special tokens and clean up tokenization spaces.
    /// </summary>
    /// <param name="tokenIds">list of tokenized input ids. Can be obtained using the `Encode` or `EncodePlus` methods.</param>
    /// <param name="skipSpecialTokens">if set to True, will replace special tokens.</param>
    /// <param name="cleanUpTokenizationSpaces">if set to True, will clean up the tokenization spaces.</param>
    /// <returns>`string`: decoded sentence</returns>
    public string Decode(List<long> tokenIds, bool skipSpecialTokens, bool cleanUpTokenizationSpaces)
    {
        var tokens = DecodeToVec(tokenIds, skipSpecialTokens);
        var decodedString = ConvertTokensToString(tokens);
        if (cleanUpTokenizationSpaces)
        {
            return CleanUpTokenization(decodedString);
        }
        else
        {
            return decodedString;
        }
    }

    /// <summary>
    /// Converts a sequence of strings into a single string. This will clean-up artifacts from tokenization
    /// (for example `sub ##word`) and generate a single output string
    /// </summary>
    /// <param name="tokens">list of tokens to concatenate.</param>
    /// <returns>`string`: concatenated sentence string</returns>
    public string ConvertTokensToString(List<string> tokens)
    {
        return string.Join(" ", tokens);
    }

    /// <summary>
    /// Cleans-up tokenization artifacts (for example whitespace before punctuation)
    /// </summary>
    /// <param name="inputString">input string to clean up</param>
    /// <returns>`string`: clean-up string</returns>
    public string CleanUpTokenization(string inputString)
    {
        return inputString
            .Replace(" .", ".")
            .Replace(" !", "!")
            .Replace(" ?", "?")
            .Replace(" ,", ",")
            .Replace(" ' ", "'")
            .Replace(" n't", "n't")
            .Replace(" 'm", "'m")
            .Replace(" do not", " don't")
            .Replace(" 's", "'s")
            .Replace(" 've", "'ve")
            .Replace(" 're", "'re");
    }

    private static List<Token> WhitespaceTokenize(Token initialToken)
    {
        var parts = initialToken.Text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var tokens = new List<Token>();
        foreach (var part in parts)
        {
            var offsets = initialToken.ReferenceOffsets.SkipWhile((offset, index) => char.IsWhiteSpace(initialToken.Text[index])).Take(part.Length).ToArray();
            tokens.Add(new Token(part, offsets));
        }
        return tokens;
    }

    protected static List<Token> SplitOnSpecialTokensV0(Token token, IVocab vocab)
    {
        var tokens = new List<Token>();
        var text = token.Text;
        var start = 0;
        var end = 0;
        while (start < text.Length)
        {
            var subText = text.Substring(start);
            var match = vocab.SpecialTokens().FirstOrDefault(t => subText.StartsWith(t));
            if (match != null)
            {
                end = start + match.Length;
                var offsets = token.ReferenceOffsets.Skip(start).Take(match.Length).ToArray();
                tokens.Add(new Token(match, offsets) { Mask = Mask.Special });
                start = end;
            }
            else
            {
                end = start + 1;
                while (end < text.Length && vocab.SpecialTokens().All(t => !text.Substring(start, end - start + 1).EndsWith(t)))
                {
                    end++;
                }

                // CONTINUED

                var offsets = token.ReferenceOffsets.Skip(start).Take(end - start).ToArray();
                tokens.Add(new Token(text.Substring(start, end - start), offsets));
                start = end;
            }
        }
        return tokens;
    }

    protected static List<Token> SplitOnSpecialTokens(Token token, IVocab vocab)
    {

        Func<string, (int, int, Mask)> testSubstr = (s) =>
        {
            foreach (var specialValue in vocab.SpecialValues.Keys)
            {
                if (s.StartsWith(specialValue))
                {
                    return (
                        specialValue.Length,
                        specialValue.ToCharArray().Count(),
                        vocab.GetUnknownValue() == specialValue ? Mask.Unknown : Mask.Special
                    );
                }
            }
            return (0, 0, Mask.None);
        };
        return SplitOnSubstr(token, testSubstr, true);
    }

    public static List<Token> SplitOnSubstr(Token token, Func<string, (int, int, Mask)> testSubstr, bool addSeparators)
    {
        List<Token> tokens = new List<Token>();
        uint charBegin = 0;
        int bytesBegin = 0;
        int charCount = 0;

        if (token.Mask == Mask.None)
        {
            var utf8Bytes = TokenizationUtils.GetUtf8Bytes(token.Text);
            var utf8Chars = TokenizationUtils.GetUtf8Chars(utf8Bytes);
            var elementsLength = TokenizationUtils.GetLengthInTextElements(token.Text);
            // Iterate over characters with byte indices
            var itr = TokenizationUtils.Enumerate(TokenizationUtils.CharIndicesForRunes(token.Text));
            foreach (var (charIdx, (bytesIdx, _)) in itr)
            {
                charCount++;
                (int matchedBytes, int matchedChars, Mask setMask) = testSubstr(token.Text.Substring(Math.Min(bytesIdx, token.Text.Length)));

                if (matchedChars > 0)
                {
                    if (charBegin < charIdx)
                    {
                        // Add previous token
                        string trimmedText = token.Text.Substring(bytesBegin, bytesIdx - bytesBegin).TrimEnd();
                        if (trimmedText.Length > 0)
                        {
                            tokens.Add(new Token(trimmedText)
                            {
                                Offset = new Offset(token.Offset.Begin + charBegin, token.Offset.Begin + charBegin + (uint)trimmedText.Length),
                                ReferenceOffsets = token.ReferenceOffsets.Skip((int)charBegin).Take(trimmedText.Length).ToArray(),
                                Mask = Mask.None
                            });
                        }
                    }

                    if (addSeparators)
                    {
                        // Add separator token
                        tokens.Add(new Token(token.Text.Substring(bytesIdx, matchedBytes))
                        {
                            Offset = new Offset(token.Offset.Begin + (uint)charIdx, token.Offset.Begin + (uint)charIdx + (uint)matchedChars),
                            ReferenceOffsets = token.ReferenceOffsets.Skip(charIdx).Take(matchedChars).ToArray(),
                            Mask = setMask
                        });
                    }

                    // Reset indices
                    charBegin = (uint)charIdx + (uint)matchedChars;
                    bytesBegin = bytesIdx + matchedBytes;
                }
            }
        }
        var utf8BytesCount = TokenizationUtils.GetUtf8BytesCount(token.Text);
        if (bytesBegin < utf8BytesCount)
        {
            // Add last buffered token if there is anything left
            int bytesIdx = utf8BytesCount;
            string text = token.Text.Substring(bytesBegin, Math.Min(bytesIdx - bytesBegin, token.Text.Length));
            if (charCount == 0)
            {
                charCount = TokenizationUtils.GetLengthInTextElements(token.Text);
            }
            tokens.Add(new Token(text)
            {
                Text = text,
                Offset = new Offset((uint)(token.Offset.Begin + charBegin), (uint)(token.Offset.Begin + charCount)),
                ReferenceOffsets = token.ReferenceOffsets.Skip((int)charBegin).Take(charCount).ToArray(),
                Mask = Mask.None
            });
        }
        return tokens;
    }



    private static List<Token> SplitOnPunct(Token token)
    {
        var tokens = new List<Token>();
        var text = token.Text;
        var start = 0;
        var end = 0;
        while (start < text.Length)
        {
            char charCurrent = text[start];
            if (char.IsPunctuation(charCurrent))
            {
                var offsets = token.ReferenceOffsets.Skip(start).Take(1).ToArray();
                tokens.Add(new Token(text.Substring(start, 1), offsets) { Mask = Mask.Punctuation });
                start++;
            }
            else
            {
                end = start + 1;
                while (end < text.Length && !char.IsPunctuation(text[end]))
                {
                    end++;
                }
                var offsets = token.ReferenceOffsets.Skip(start).Take(end - start).ToArray();
                tokens.Add(new Token(text.Substring(start, end - start), offsets));
                start = end;
            }
        }
        return tokens;
    }

    private static List<Token> TokenizeCjkChars(Token token)
    {
        var tokens = new List<Token>();
        var text = token.Text;
        var start = 0;
        var end = 0;
        while (start < text.Length)
        {
            char charCurrent = text[start];
            if (IsCjkChar(charCurrent))
            {
                var offsets = token.ReferenceOffsets.Skip(start).Take(1).ToArray();
                tokens.Add(new Token(text.Substring(start, 1), offsets) { Mask = Mask.CJK });
                start++;
            }
            else
            {
                end = start + 1;
                while (end < text.Length && !IsCjkChar(text[end]))
                {
                    end++;
                }
                var offsets = token.ReferenceOffsets.Skip(start).Take(end - start).ToArray();
                tokens.Add(new Token(text.Substring(start, end - start), offsets));
                start = end;
            }
        }
        return tokens;
    }

    private static bool IsCjkChar(char c)
    {
        var unicodeBlock = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
        return unicodeBlock == System.Globalization.UnicodeCategory.OtherLetter;
    }

    private static void CleanText(Token token, bool removeControlCharacters)
    {
        if (removeControlCharacters)
        {
            token.Text = Regex.Replace(token.Text, @"\p{C}+", "");
        }
        token.Text = token.Text.Replace("``", "\"").Replace("''", "\"");
    }

    private static void Lowercase(Token token)
    {
        token.Text = token.Text.ToLowerInvariant();
    }

    private static void StripAccents(Token token)
    {
        token.Text = RemoveDiacritics(token.Text);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private List<string> DecodeToVec(List<long> tokenIds, bool skipSpecialTokens)
    {
        var tokens = new List<string>();
        if (skipSpecialTokens)
        {
            tokens = tokenIds.Where(id => !_vocab.SpecialIndices.ContainsKey(id)).Select(id => _vocab.IdToToken(id)).ToList();
        }
        else
        {
            tokens = tokenIds.Select(id => _vocab.IdToToken(id)).ToList();
        }
        return tokens;
    }
}

public interface IMultiThreadedTokenizer<T> where T : IVocab
{
    T Vocab();

    List<TokensWithOffsets> TokenizeListWithOffsets(string[] textList);

    List<List<string>> TokenizeList(string[] textList);

    List<TokenizedInput> EncodeList(
        string[] textList,
        int maxLen,
        TruncationStrategy truncationStrategy,
        int stride
    );

    List<TokenizedInput> EncodePairList(
        Tuple<string, string>[] textList,
        int maxLen,
        TruncationStrategy truncationStrategy,
        int stride
    );

    List<string> DecodeList(
        List<List<long>> tokenIdsList,
        bool skipSpecialTokens,
        bool cleanUpTokenizationSpaces
    );
}
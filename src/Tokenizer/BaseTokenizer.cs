using System.Dynamic;
using System.Text;
using Lokad.Tokenizers.Vocab;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;

namespace Lokad.Tokenizers.Tokenizer;

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
    private readonly T _vocab;
    private readonly bool _lowerCase;
    private readonly bool _stripAccents;

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
    /// Tokenize a string, returns a list of tokens as strings.
    /// </summary>
    public List<string> Tokenize(string text)
    {
        return TokenizeWithOffsets(text).Tokens;
    }

    /// <summary>
    /// Tokenize a string, returning tokens with offset information
    /// </summary>
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

        var initialOffsets = Enumerable.Range(0, text.EnumerateRunes().Count()).Select(i => (uint)i).ToArray();
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

    public void DecomposeNfkc(Token token)
    {
        // Perform NFKC normalization on the token text
        var decomposedText = token.Text.Normalize(NormalizationForm.FormKC);

        // Calculate the new reference offsets
        var newReferenceOffsets = new List<uint>();
        uint currentOffset = 0;
        for (var i = 0; i < decomposedText.Length; i++)
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

    public TokenizedInput Encode(String text1, String? text2, int maxLen, TruncationStrategy truncationStrategy,
        int stride)
    {
        var tokens = TokenizeWithOffsets(text1);
        var token_ids_1 = ConvertTokensToIds(tokens.Tokens);
        var len_1 = token_ids_1.Count;

        var token_ids_with_offsets_1 = new TokenIdsWithOffsets
        {
            Ids = token_ids_1,
            Offsets = tokens.Offsets,
            ReferenceOffsets = tokens.ReferenceOffsets.Select(_ => _.ToList()).ToList(),
            Masks = tokens.Masks,
        };

        TokenIdsWithOffsets? token_ids_with_offsets_2 = null;
        var len_2 = 0;
        if (text2 != null)
        {
            var tokens2 = TokenizeWithOffsets(text2);
            var token_ids_2 = ConvertTokensToIds(tokens2.Tokens);
            len_2 = token_ids_2.Count;
            token_ids_with_offsets_2 = text2 == null ? null : new TokenIdsWithOffsets()
            {
                Ids = token_ids_2,
                Offsets = tokens2.Offsets,
                ReferenceOffsets = tokens2.ReferenceOffsets.Select(_ => _.ToList()).ToList(),
                Masks = tokens2.Masks,
            };
        }

        var additional_tokens = BuildInputWithSpecialTokens(
            tokens1: new TokenIdsWithOffsets
            {
                Ids = new List<long>(),
                Masks = new List<Mask>(),
                Offsets = new List<Offset?>(),
                ReferenceOffsets = new List<List<uint>>()
            },
            tokens2: token_ids_with_offsets_2 == null ? null : new TokenIdsWithOffsets
            {
                Ids = new List<long>(),
                Masks = new List<Mask>(),
                Offsets = new List<Offset?>(),
                ReferenceOffsets = new List<List<uint>>()
            }
            );


        var total_len = len_1 + len_2 + additional_tokens.TokenIds.Count;
        var num_truncated_tokens = total_len > maxLen ? total_len - maxLen : 0;

        var (token_ids_with_offsets_1_x, token_ids_with_offsets_2_x, overflowing_tokens, _overflowing_offsets) =
            TokenizationUtils.TruncateSequences(token_ids_with_offsets_1, token_ids_with_offsets_2,
                num_truncated_tokens, truncationStrategy, stride);
        token_ids_with_offsets_1 = token_ids_with_offsets_1_x;

        var merged_tokenized_input =
            BuildInputWithSpecialTokens(token_ids_with_offsets_1, token_ids_with_offsets_2);

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
    /// Builds input with special tokens.
    /// </summary>
    public virtual TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets tokens1, TokenIdsWithOffsets tokens2 = null)
    {
        var tokenSegmentIds = new List<byte>(new byte[tokens1.Ids.Count]);
        var specialTokensMask = new List<byte>(new byte[tokens1.Ids.Count]);

        if (tokens2 != null)
        {
            var tokensIdsWithOffsets2Value = tokens2;
            var length = tokensIdsWithOffsets2Value.Ids.Count;

            tokenSegmentIds.AddRange(Enumerable.Repeat((byte)1, length));
            specialTokensMask.AddRange(Enumerable.Repeat((byte)0, length));

            tokens1.Ids.AddRange(tokensIdsWithOffsets2Value.Ids);
            tokens1.Offsets.AddRange(tokensIdsWithOffsets2Value.Offsets);
            tokens1.ReferenceOffsets.AddRange(tokensIdsWithOffsets2Value.ReferenceOffsets);
            tokens1.Masks.AddRange(tokensIdsWithOffsets2Value.Masks);
        }

        return new TokenIdsWithSpecialTokens
        {
            TokenIds = tokens1.Ids,
            SegmentIds = tokenSegmentIds,
            SpecialTokensMask = specialTokensMask,
            TokenOffsets = tokens1.Offsets,
            ReferenceOffsets = tokens1.ReferenceOffsets,
            Mask = tokens1.Masks
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
    private string ConvertTokensToString(List<string> tokens)
    {
        return string.Join(" ", tokens);
    }


    // Protected methods
    protected List<Token> SplitOnSpecialTokens(Token token, IVocab vocab)
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
    
    // Private methods
    
    /// <summary>
    /// Cleans-up tokenization artifacts (for example whitespace before punctuation)
    /// </summary>
    /// <param name="inputString">input string to clean up</param>
    /// <returns>`string`: clean-up string</returns>
    private string CleanUpTokenization(string inputString)
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

    private List<Token> WhitespaceTokenize(Token initialToken)
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

    private List<Token> SplitOnSubstr(Token token, Func<string, (int, int, Mask)> testSubstr, bool addSeparators)
    {
        var tokens = new List<Token>();
        uint charBegin = 0;
        var bytesBegin = 0;
        var charCount = 0;

        if (token.Mask == Mask.None)
        {
            // Iterate over characters with byte indices
            var itr = TokenizationUtils.Enumerate(TokenizationUtils.CharIndicesForRunes(token.Text));
            foreach (var (charIdx, (bytesIdx, _)) in itr)
            {
                charCount++;
                (var matchedBytes, var matchedChars, var setMask) = testSubstr(TokenizationUtils.SubstringRunes(token.Text, bytesIdx));

                if (matchedChars > 0)
                {
                    if (charBegin < charIdx)
                    {
                        // Add previous token
                        var trimmedText = TokenizationUtils.SubstringRunes(token.Text, bytesBegin, bytesIdx - bytesBegin).TrimEnd();
                        if (trimmedText.EnumerateRunes().Count() > 0)
                        {
                            tokens.Add(new Token(trimmedText)
                            {
                                Offset = new Offset(token.Offset.Begin + charBegin, token.Offset.Begin + charBegin + (uint)trimmedText.Length),
                                ReferenceOffsets = token.ReferenceOffsets.Skip((int)charBegin).Take(trimmedText.EnumerateRunes().Count()).ToArray(),
                                Mask = Mask.None
                            });
                        }
                    }

                    if (addSeparators)
                    {
                        // Add separator token
                        tokens.Add(new Token(TokenizationUtils.SubstringRunes(token.Text, bytesIdx, matchedBytes))
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
            var bytesIdx = utf8BytesCount;
            var text = TokenizationUtils.SubstringRunes(token.Text, bytesBegin, bytesBegin + (bytesIdx - bytesBegin));
            if (charCount == 0)
            {
                charCount = token.Text.EnumerateRunes().Count();
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

    private List<Token> SplitOnPunct(Token token)
    {
        var tokens = new List<Token>();
        var text = token.Text;
        var start = 0;
        var end = 0;
        while (start < text.Length)
        {
            var charCurrent = text[start];
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

    private List<Token> TokenizeCjkChars(Token token)
    {
        var tokens = new List<Token>();
        var text = token.Text;
        var start = 0;
        var end = 0;
        while (start < text.Length)
        {
            var charCurrent = text[start];
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

    private bool IsCjkChar(char c)
    {
        var unicodeBlock = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
        return unicodeBlock == System.Globalization.UnicodeCategory.OtherLetter;
    }

    private void CleanText(Token token, bool removeControlCharacters)
    {
        if (removeControlCharacters)
        {
            token.Text = Regex.Replace(token.Text, @"\p{C}+", "");
        }
        token.Text = token.Text.Replace("``", "\"").Replace("''", "\"");
    }
    
    private void Lowercase(Token token)
    {
        token.Text = token.Text.ToLowerInvariant();
    }

    private void StripAccents(Token token)
    {
        token.Text = RemoveDiacritics(token.Text);
    }

    private string RemoveDiacritics(string text)
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
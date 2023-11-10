using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Lokad.Tokenizers.Vocab;

namespace Lokad.Tokenizers.Tokenizer;

// TODO: WIP, ChatGPT port of https://github.com/guillaume-be/rust-tokenizers/blob/main/main/src/tokenizer/tokenization_utils.rs

//public static class TokenizationUtils
//{
//    /// <summary>
//    /// Cleans text by removing control characters and normalizing whitespace
//    /// </summary>
//    public static void CleanText(Token token, bool strict)
//    {
//        var cleanedString = new StringBuilder(token.Text.Length);
//        var characterMapping = new List<uint>(token.Text.Length);

//        foreach (var (character, position) in token.Text.Zip(token.ReferenceOffsets))
//        {
//            if (IsControl(character, strict) || character == '\x00' || character == '\uFFFD')
//            {
//                continue;
//            }

//            cleanedString.Append(IsWhitespace(character) ? ' ' : character);
//            characterMapping.Add(position);
//        }

//        token.Text = cleanedString.ToString();
//        token.ReferenceOffsets = characterMapping;
//        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
//    }

//    /// <summary>
//    /// Replaces a pattern string by a replacement string keeping track of the offsets
//    /// (all new characters in replacement have the same reference offset as the first pattern character as these may have a different size)
//    /// </summary>
//    public static void ReplaceString(Token token, string pattern, string replacementString)
//    {
//        var patternLen = pattern.Length;
//        var patternCharLen = pattern.Length;
//        var replacementCharLen = replacementString.Length;
//        var matches = token.Text.LastIndexOf(pattern).Select(v => v).ToList();
//        var charIndices = token.Text.Select((c, i) => new { Index = i, Character = c }).ToDictionary(x => x.Index, x => x.Character);

//        foreach (var hit in matches)
//        {
//            token.Text = token.Text.Remove(hit, patternLen).Insert(hit, replacementString);
//            var charPosition = charIndices[hit];
//            var referenceOffset = token.ReferenceOffsets[charPosition];
//            token.ReferenceOffsets = token.ReferenceOffsets.Take(charPosition)
//                .Concat(Enumerable.Repeat(referenceOffset, replacementCharLen))
//                .Concat(token.ReferenceOffsets.Skip(charPosition + patternCharLen)).ToList();
//        }
//    }

//    /// <summary>
//    /// Split a text on special tokens (like BOS/EOS/UNK markers), depending on the vocabulary
//    /// </summary>
//    public static List<TokenRef> SplitOnSpecialTokens(TokenRef token, IVocab vocab)
//    {
//        bool TestSubstr(string s)
//        {
//            foreach (var specialValue in vocab.SpecialValues().Keys)
//            {
//                if (s.StartsWith(specialValue))
//                {
//                    return (specialValue.Length,
//                        specialValue.Length,
//                        vocab.GetUnknownValue() == specialValue ? Mask.Unknown : Mask.Special);
//                }
//            }

//            return (0, 0, Mask.None);
//        }

//        return SplitOnSubstr(token, TestSubstr, true);
//    }

//    /// <summary>
//    /// Tokenizes CJK characters, each character will be a token
//    /// </summary>
//    public static List<TokenRef> TokenizeCjkChars(TokenRef token)
//    {
//        return SplitOnChar(token, IsCjkChar, true, Mask.CJK);
//    }

//    private static bool IsCjkChar(char character)
//    {
//        var u32Char = Convert.ToUInt32(character);
//        return (0x4E00 <= u32Char && u32Char <= 0x9FFF)
//            || (0x3400 <= u32Char && u32Char <= 0x4DBF)
//            || (0x20000 <= u32Char && u32Char <= 0x2A6DF)
//            || (0x2A700 <= u32Char && u32Char <= 0x2B73F)
//            || (0x2B740 <= u32Char && u32Char <= 0x2B81F)
//            || (0x2B820 <= u32Char && u32Char <= 0x2CEAF)
//            || (0xF900 <= u32Char && u32Char <= 0xFAFF)
//            || (0x2F800 <= u32Char && u32Char <= 0x2FA1F);
//    }

//    public static bool IsWhitespace(char character)
//    {
//        return Constants.WHITESPACE_CHARS.Contains(Convert.ToUInt32(character));
//    }

//    /// <summary>
//    /// This is a custom method to check if a character is a control character. The BERT tokenizer is
//    /// taking any character whose unicode category starts with `C` as a control character, which includes
//    /// the traditional control `Cc` category, but also the format `Cc`, private use `Co` and surrogate `Cs`.
//    /// The unassigned unicode category `Cn` has been skipped in order to avoid unnecessary checks.
//    /// A faster method may be called by setting strict to false and only check against the core control
//    /// characters. To match the original BERT tokenization, this should remain true.
//    /// </summary>
//    public static bool IsControl(char character, bool strict)
//    {
//        if (Constants.ADDITIONAL_WHITESPACE_CHARS.Contains(character))
//        {
//            return false;
//        }

//        if (strict)
//        {
//            var u32Char = Convert.ToUInt32(character);
//            return (u32Char <= 0x001F)
//                || (0x0080 <= u32Char && u32Char <= 0x009F)
//                || (0xE0020 <= u32Char && u32Char <= 0xE007F)
//                || (0xE000 <= u32Char && u32Char <= 0xF8FF)
//                || (0xF0000 <= u32Char && u32Char <= 0xFFFFD)
//                || (0x100000 <= u32Char && u32Char <= 0x10FFFD)
//                || (0xD800 <= u32Char && u32Char <= 0xDB7F)
//                || (0xDB80 <= u32Char && u32Char <= 0xDBFF)
//                || (0xDC00 <= u32Char && u32Char <= 0xDFFF)
//                || Constants.CONTROL_CHARS.Contains(u32Char);
//        }
//        else
//        {
//            return char.IsControl(character);
//        }
//    }

//    public static bool IsPunctuation(char character)
//    {
//        var u32Char = Convert.ToUInt32(character);
//        return (33 <= u32Char && u32Char <= 47)
//            || (58 <= u32Char && u32Char <= 64)
//            || (91 <= u32Char && u32Char <= 96)
//            || (123 <= u32Char && u32Char <= 126)
//            || Constants.PUNCTUATION_CHARS.Contains(u32Char);
//    }

//    /// <summary>
//    /// Simple tokenization based on whitespace only
//    /// </summary>
//    public static List<TokenRef> WhitespaceTokenize(TokenRef token)
//    {
//        return SplitOnChar(token, IsWhitespace, false, Mask.Whitespace);
//    }

//    /// <summary>
//    /// Lowercase
//    /// </summary>
//    public static void Lowercase(Token token)
//    {
//        var lowerCasedString = new StringBuilder(token.Text.Length);
//        var characterMapping = new List<uint>(token.Text.Length);

//        foreach (var (character, position) in token.Text.Zip(token.ReferenceOffsets))
//        {
//            foreach (var c in character.ToString().ToLower())
//            {
//                lowerCasedString.Append(c);
//                characterMapping.Add(position);
//            }
//        }

//        token.Text = lowerCasedString.ToString();
//        token.ReferenceOffsets = characterMapping;
//        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
//    }

//    /// <summary>
//    /// Remove diacritics
//    /// </summary>
//    public static void StripAccents(Token token)
//    {
//        var decomposedString = new StringBuilder(token.Text.Length);
//        var characterMapping = new List<uint>(token.Text.Length);

//        foreach (var (character, position) in token.Text.Zip(token.ReferenceOffsets))
//        {
//            foreach (var c in character.ToString().Normalize(NormalizationForm.FormD))
//            {
//                if (!Constants.ACCENT_MARKERS.Contains(Convert.ToUInt32(c)))
//                {
//                    decomposedString.Append(c);
//                    characterMapping.Add(position);
//                }
//            }
//        }

//        token.Text = decomposedString.ToString();
//        token.ReferenceOffsets = characterMapping;
//        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
//    }

//    /// <summary>
//    /// NFKC decomposition
//    /// </summary>
//    public static void DecomposeNfkc(Token token)
//    {
//        var decomposedString = new StringBuilder(token.Text.Length);
//        var characterMapping = new List<uint>(token.Text.Length);
//        var curPosition = 0;

//        foreach (var (character, extraChar) in token.Text.Normalize(NormalizationForm.FormKC))
//        {
//            decomposedString.Append(character);
//            if (extraChar > 0)
//            {
//                curPosition -= extraChar;
//            }
//            characterMapping.Add(token.ReferenceOffsets[curPosition]);
//            if (extraChar < 0)
//            {
//                curPosition -= extraChar;
//            }
//            curPosition += 1;
//        }

//        token.Text = decomposedString.ToString();
//        token.ReferenceOffsets = characterMapping;
//        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
//    }

//    /// <summary>
//    /// Split a token on punctuation
//    /// </summary>
//    public static List<TokenRef> SplitOnPunct(TokenRef token)
//    {
//        return SplitOnChar(token, IsPunctuation, true, Mask.Punctuation);
//    }

//    /// <summary>
//    /// Split a token on one or more characters (given a character test function)
//    /// </summary>
//    public static List<TokenRef> SplitOnChar(TokenRef token, Func<char, bool> testCharacter, bool addSeparators, Mask setMask)
//    {
//        var tokens = new List<TokenRef>();
//        var charBegin = 0;
//        var bytesBegin = 0;
//        var charCount = 0;

//        if (token.Mask == Mask.None)
//        {
//            foreach (var (charIdx, (bytesIdx, c)) in token.Text.Select((c, i) => new { Index = i, Character = c }).ToList())
//            {
//                charCount += 1;
//                if (testCharacter(c))
//                {
//                    if (charBegin < charIdx)
//                    {
//                        tokens.Add(new TokenRef
//                        {
//                            Text = token.Text.Substring(bytesBegin, bytesIdx - bytesBegin),
//                            Offset = new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)charIdx),
//                            ReferenceOffsets = token.ReferenceOffsets.Skip(charBegin).Take(charIdx - charBegin).ToArray(),
//                            Mask = Mask.None
//                        });
//                    }
//                    if (addSeparators)
//                    {
//                        tokens.Add(new TokenRef
//                        {
//                            Text = token.Text.Substring(bytesIdx, c.ToString().Length),
//                            Offset = new Offset(token.Offset.Begin + (uint)charIdx, token.Offset.Begin + (uint)charIdx + 1),
//                            ReferenceOffsets = token.ReferenceOffsets.Skip(charIdx).Take(1).ToArray(),
//                            Mask = setMask
//                        });
//                    }
//                    charBegin = charIdx + 1;
//                    bytesBegin = bytesIdx + c.ToString().Length;
//                }
//            }
//        }
//        if (bytesBegin < token.Text.Length)
//        {
//            var bytesIdx = token.Text.Length;
//            tokens.Add(new TokenRef
//            {
//                Text = token.Text.Substring(bytesBegin, bytesIdx - bytesBegin),
//                Offset = new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)charCount),
//                ReferenceOffsets = token.ReferenceOffsets.Skip(charBegin).Take(charCount - charBegin).ToArray(),
//                Mask = Mask.None
//            });
//        }
//        return tokens;
//    }

//    public static List<TokenRef> SplitOnRegexWithLookahead(TokenRef token, Regex patternLookahead, Regex patternTokenization)
//    {
//        if (token.Mask == Mask.None)
//        {
//            var subWords = new List<string>();
//            var splits = new List<string>();

//            var i = 0;
//            var endByte = 0;
//            foreach (var hit in patternLookahead.Matches(token.Text))
//            {
//                var hitChars = hit.Value.Reverse().ToList();
//                var start = hitChars[0];
//                var sep = hitChars[1];
//                endByte = hit.Index + hit.Length - sep.ToString().Length - start.ToString().Length;
//                splits.Add(token.Text.Substring(i, endByte - i));
//                i = endByte;
//            }
//            splits.Add(token.Text.Substring(i));

//            foreach (var subWord in splits)
//            {
//                foreach (var hit in patternTokenization.Matches(subWord))
//                {
//                    subWords.Add(hit.Value);
//                }
//            }

//            var outputTokens = new List<TokenRef>(subWords.Count);
//            var beginChar = 0;
//            foreach (var (subWord, idx) in subWords.Select((v, i) => new { Value = v, Index = i }))
//            {
//                var endChar = beginChar + subWord.Length;
//                outputTokens.Add(new TokenRef
//                {
//                    Text = subWord,
//                    Offset = new Offset(token.Offset.Begin + (uint)beginChar, token.Offset.Begin + (uint)endChar),
//                    ReferenceOffsets = token.ReferenceOffsets.Skip(beginChar).Take(endChar - beginChar).ToArray(),
//                    Mask = Mask.None
//                });
//                beginChar = endChar;
//            }

//            return outputTokens;
//        }
//        else
//        {
//            return new List<TokenRef> { token };
//        }
//    }

//    public static List<TokenRef> SplitOnRegex(TokenRef token, Regex patternTokenization)
//    {
//        var tokens = new List<TokenRef>();
//        var beginChar = 0;
//        foreach (var hit in patternTokenization.Matches(token.Text))
//        {
//            var startByte = hit.Index;
//            if (startByte > 0)
//            {
//                beginChar = token.Text.Substring(0, startByte).Length;
//            }
//            var endChar = beginChar + hit.Value.Length;
//            tokens.Add(new TokenRef
//            {
//                Text = hit.Value,
//                Offset = new Offset(token.Offset.Begin + (uint)beginChar, token.Offset.Begin + (uint)endChar),
//                ReferenceOffsets = token.ReferenceOffsets.Skip(beginChar).Take(endChar - beginChar).ToArray(),
//                Mask = Mask.None
//            });
//            beginChar = endChar;
//        }
//        return tokens;
//    }

//    public static List<TokenRef> SplitAtRegex(TokenRef token, Regex patternTokenization)
//    {
//        var tokens = new List<TokenRef>();
//        var beginChar = 0;
//        var startByte = 0;
//        foreach (var hit in patternTokenization.Matches(token.Text))
//        {
//            var hitStartByte = hit.Index;
//            var hitStartChar = token.Text.Substring(0, hitStartByte).Length;
//            var hitEndByte = hit.Index + hit.Length;
//            var hitEndChar = beginChar + hit.Value.Length;

//            if (!string.IsNullOrWhiteSpace(token.Text.Substring(startByte, hitStartByte - startByte)))
//            {
//                tokens.Add(new TokenRef
//                {
//                    Text = token.Text.Substring(startByte, hitStartByte - startByte),
//                    Offset = new Offset(token.Offset.Begin + (uint)beginChar, token.Offset.Begin + (uint)hitStartChar),
//                    ReferenceOffsets = token.ReferenceOffsets.Skip(beginChar).Take(hitStartChar - beginChar).ToArray(),
//                    Mask = Mask.None
//                });
//            }

//            tokens.Add(new TokenRef
//            {
//                Text = hit.Value,
//                Offset = new Offset(token.Offset.Begin + (uint)hitStartChar, token.Offset.Begin + (uint)hitEndChar),
//                ReferenceOffsets = token.ReferenceOffsets.Skip(hitStartChar).Take(hitEndChar - hitStartChar).ToArray(),
//                Mask = Mask.None
//            });
//            beginChar = hitEndChar;
//            startByte = hitEndByte;
//        }
//        if (!string.IsNullOrWhiteSpace(token.Text.Substring(startByte)))
//        {
//            tokens.Add(new TokenRef
//            {
//                Text = token.Text.Substring(startByte),
//                Offset = new Offset(token.Offset.Begin + (uint)beginChar, (uint)token.Text.Length),
//                ReferenceOffsets = token.ReferenceOffsets.Skip(startByte).ToArray(),
//                Mask = Mask.None
//            });
//        }

//        return tokens;
//    }

//    /// <summary>
//    /// Split a token on one or more substrings (given a substring test function)
//    /// </summary>
//    public static List<TokenRef> SplitOnSubstr(TokenRef token, Func<string, (int, int, Mask)> testSubstr, bool addSeparators)
//    {
//        var tokens = new List<TokenRef>();
//        var charBegin = 0;
//        var bytesBegin = 0;
//        var charCount = 0;

//        if (token.Mask == Mask.None)
//        {
//            foreach (var (charIdx, (bytesIdx, _)) in token.Text.Select((c, i) => new { Index = i, Character = c }).ToList())
//            {
//                charCount += 1;
//                var (matchedBytes, matchedChars, setMask) = testSubstr(token.Text.Substring(bytesIdx));
//                if (matchedChars > 0)
//                {
//                    if (charBegin < charIdx)
//                    {
//                        tokens.Add(new TokenRef
//                        {
//                            Text = token.Text.Substring(bytesBegin, bytesIdx - bytesBegin),
//                            Offset = new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)charIdx),
//                            ReferenceOffsets = token.ReferenceOffsets.Skip(charBegin).Take(charIdx - charBegin).ToArray(),
//                            Mask = Mask.None
//                        });
//                    }
//                    if (addSeparators)
//                    {
//                        tokens.Add(new TokenRef
//                        {
//                            Text = token.Text.Substring(bytesIdx, matchedBytes),
//                            Offset = new Offset(token.Offset.Begin + (uint)charIdx, token.Offset.Begin + (uint)charIdx + matchedChars),
//                            ReferenceOffsets = token.ReferenceOffsets.Skip(charIdx).Take(matchedChars).ToArray(),
//                            Mask = setMask
//                        });
//                    }
//                    charBegin = charIdx + matchedChars;
//                    bytesBegin = bytesIdx + matchedBytes;
//                }
//            }
//        }
//        if (bytesBegin < token.Text.Length)
//        {
//            var bytesIdx = token.Text.Length;
//            tokens.Add(new TokenRef
//            {
//                Text = token.Text.Substring(bytesBegin, bytesIdx - bytesBegin),
//                Offset = new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)charCount),
//                ReferenceOffsets = token.ReferenceOffsets.Skip(charBegin).Take(charCount - charBegin).ToArray(),
//                Mask = Mask.None
//            });
//        }
//        return tokens;
//    }

//    /// <summary>
//    /// Tokenize a token into word pieces according to the supplied vocabulary
//    /// Continuation word pieces will all have the suffix `##`
//    /// </summary>
//    public static List<Token> TokenizeWordpiece(TokenRef token, IVocab vocab, int maxWordLen)
//    {
//        var tokens = new List<Token>();
//        if (token.Text.Length > maxWordLen)
//        {
//            tokens.Add(new Token
//            {
//                Text = vocab.GetUnknownValue(),
//                Offset = token.Offset,
//                ReferenceOffsets = token.ReferenceOffsets.ToList(),
//                Mask = Mask.Unknown
//            });
//        }
//        else
//        {
//            var charIndices = token.Text.Select((c, i) => new { Index = i, Character = c }).ToDictionary(x => x.Index, x => x.Character);
//            var maxEnd = charIndices.Last().Key + token.Text.Last().ToString().Length;
//            var start = 0;
//            var posBegin = 0;
//            var posEnd = 0;
//            var end = 0;

//            while (start < maxEnd)
//            {
//                end = maxEnd;
//                posEnd = charIndices.Count;
//                var isUnk = true;

//                while (start < end)
//                {
//                    var substr = token.Text.Substring(start, end - start);
//                    var charLength = substr.Length;
//                    var subOffset = new Offset(token.Offset.Begin + (uint)posBegin, token.Offset.Begin + (uint)posBegin + (uint)charLength);

//                    if (start > 0)
//                    {
//                        substr = "##" + substr;
//                    }

//                    if (vocab.Values().ContainsKey(substr))
//                    {
//                        tokens.Add(new Token
//                        {
//                            Text = substr,
//                            Offset = subOffset,
//                            ReferenceOffsets = token.ReferenceOffsets.Skip(posBegin).Take(charLength).ToList(),
//                            Mask = start > 0 ? Mask.Continuation : token.Mask
//                        });
//                        isUnk = false;
//                        break;
//                    }

//                    posEnd -= 1;
//                    end = charIndices[posEnd].Key;
//                }

//                if (isUnk)
//                {
//                    return new List<Token>
//                        {
//                            new Token
//                            {
//                                Text = vocab.GetUnknownValue(),
//                                Offset = token.Offset,
//                                ReferenceOffsets = token.ReferenceOffsets.ToList(),
//                                Mask = Mask.Unknown
//                            }
//                        };
//                }

//                start = end;
//                posBegin = posEnd;
//            }

//            FixMask(tokens);
//        }

//        return tokens;
//    }

//    private static void FixMask(List<Token> tokens)
//    {
//        for (var i = 1; i < tokens.Count; i++)
//        {
//            if (tokens[i].Mask == Mask.Continuation && tokens[i - 1].Mask == Mask.None)
//            {
//                tokens[i - 1].Mask = Mask.Begin;
//            }
//        }
//    }

//    public static List<TokenRef> SplitOnLanguageCode(TokenRef token, int codeLength, HashSet<List<byte>> languageCodesBytes)
//    {
//        if (token.Text.Length < codeLength)
//        {
//            return new List<TokenRef> { token };
//        }

//        var tokens = new List<TokenRef>();
//        var beginChar = 0;
//        var startByte = 0;

//        foreach (var (cStart, c) in token.Text.Select((c, i) => new { Index = i, Character = c }).ToList())
//        {
//            if (!char.IsWhiteSpace(c))
//            {
//                break;
//            }
//            startByte = cStart;
//            beginChar += 1;
//        }

//        var leadingBytes = Encoding.UTF8.GetBytes(token.Text.Substring(startByte, codeLength));

//        if (languageCodesBytes.Contains(leadingBytes.ToList()))
//        {
//            tokens.Add(new TokenRef
//            {
//                Text = token.Text.Substring(startByte, codeLength),
//                Offset = new Offset(token.Offset.Begin + (uint)beginChar, token.Offset.Begin + (uint)beginChar + (uint)codeLength),
//                ReferenceOffsets = token.ReferenceOffsets.Skip(beginChar).Take(codeLength).ToArray(),
//                Mask = Mask.Special
//            });

//            startByte += codeLength;
//            beginChar += codeLength;

//            foreach (var (cStart, c) in token.Text.Substring(startByte).Select((c, i) => new { Index = i, Character = c }).ToList())
//            {
//                if (!char.IsWhiteSpace(c))
//                {
//                    break;
//                }
//                startByte = cStart;
//                beginChar += 1;
//            }
//        }

//        tokens.Add(new TokenRef
//        {
//            Text = token.Text.Substring(startByte),
//            Offset = new Offset(token.Offset.Begin + (uint)beginChar, (uint)token.Text.Length),
//            ReferenceOffsets = token.ReferenceOffsets.Skip(beginChar).ToArray(),
//            Mask = Mask.None
//        });

//        return tokens;
//    }

//    public static List<Token> UnknownByteFallback(TokenRef token, IVocab vocab)
//    {
//        if (!vocab.Values().ContainsKey(token.Text))
//        {
//            return token.Text.Select(c => new Token
//            {
//                Text = $"<{Convert.ToByte(c):X4}>",
//                Offset = new Offset(token.Offset.End, token.Offset.End),
//                ReferenceOffsets = new List<uint> { token.ReferenceOffsets.Last() },
//                Mask = token.Mask
//            }).ToList();
//        }

//        return null;
//    }
//}
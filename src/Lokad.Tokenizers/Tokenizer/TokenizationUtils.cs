using System.Text.RegularExpressions;
using System.Text;
using Lokad.Tokenizers.Vocab;
using System.Globalization;

namespace Lokad.Tokenizers.Tokenizer;

// TODO: ChatGPT port of https://github.com/guillaume-be/rust-tokenizers/blob/main/main/src/tokenizer/tokenization_utils.rs
// TODO: unit tests not ported

public static class TokenizationUtils
{
    // Substring Runes (characters)
    public static string SubstringRunes(string text, int start, int length)
    {
        var sb = new StringBuilder();
        text.EnumerateRunes().Skip(start).Take(length).ToList().ForEach(r => sb.Append(r));
        return sb.ToString();
    }

    public static string SubstringRunes(string text, int start)
    {
        var sb = new StringBuilder();
        text.EnumerateRunes().Skip(start).ToList().ForEach(r => sb.Append(r));
        return sb.ToString();
    }




    // Get string Runes (characters)
    public static StringRuneEnumerator GetStringRuneEnumerator(string text)
    {
        return text.EnumerateRunes();
    }

    // Get LengthInTextElements
    public static StringInfo GetStringInfo(string text)
    {
        return new System.Globalization.StringInfo(text);
    }

    // Get LengthInTextElements
    public static int GetLengthInTextElements(string text)
    {
        return GetStringInfo(text).LengthInTextElements;
    }

    // Get UTF 8 Bytes
    public static byte[] GetUtf8Bytes(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }

    // Get UTF 8 Chars
    public static char[] GetUtf8Chars(byte[] bytes)
    {
        return Encoding.UTF8.GetChars(bytes);
    }

    // Get UTF 8 Bytes Count
    public static int GetUtf8BytesCount(string text)
    {
        return Encoding.UTF8.GetByteCount(text);
    }

    public static IEnumerable<(int Index, char Character)> CharIndices(string str)
    {
        int index = 0;
        foreach (char character in str)
        {
            yield return (index, character);
            index += GetUtf8BytesCount(character.ToString());
        }
    }

    public static IEnumerable<(int Index, string Character)> CharIndicesForTextElements(string str)
    {
        int index = 0;
        var strInfo = GetStringInfo(str);
        for (int i = 0; i < strInfo.LengthInTextElements; i++)
        {
            var c = strInfo.SubstringByTextElements(i, 1);
            yield return (index, c);
            //index += GetUtf8BytesCount(strInfo.SubstringByTextElements(i, 1));
            index++;
        }
    }

    public static IEnumerable<(int Index, Rune Character)> CharIndicesForRunes(string str)
    {
        int index = 0;
        var runes = str.EnumerateRunes();
        for (int i = 0; i < runes.Count(); i++)
        {
            var c = runes.ElementAt(i);
            yield return (index, c);
            //index += GetUtf8BytesCount(strInfo.SubstringByTextElements(i, 1));
            index++;
        }
    }

    public static IEnumerable<(int Index, T Element)> Enumerate<T>(IEnumerable<T> source)
    {
        int index = 0;
        foreach (var element in source)
        {
            yield return (index++, element);
        }
    }

    /// <summary>
    /// Cleans text by removing control characters and normalizing whitespace
    /// </summary>
    public static void CleanText(Token token, bool strict)
    {
        var cleanedString = new StringBuilder(token.Text.Length);
        var characterMapping = new List<uint>(token.Text.Length);

        foreach (var (character, position) in token.Text.EnumerateRunes().Zip(token.ReferenceOffsets))
        {
            if (IsControl(character, strict) || character == new Rune('\x00') || character == new Rune('\uFFFD'))
            {
                continue;
            }

            cleanedString.Append(IsWhitespace(character) ? ' ' : character);
            characterMapping.Add(position);
        }

        token.Text = cleanedString.ToString();
        token.ReferenceOffsets = characterMapping;
        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
    }

    /// <summary>
    /// Replaces a pattern string by a replacement string keeping track of the offsets
    /// (all new characters in replacement have the same reference offset as the first pattern character as these may have a different size)
    /// </summary>
    public static void ReplaceString(Token token, string pattern, string replacementString)
    {
        int patternLen = pattern.Length;
        int patternCharLen = pattern.ToCharArray().Length;
        int replacementCharLen = replacementString.ToCharArray().Length;

        // Find all matches from the end to the beginning
        List<int> matches = new List<int>();
        int index = token.Text.LastIndexOf(pattern);
        while (index != -1)
        {
            matches.Add(index);
            index = (index > 0) ? token.Text.LastIndexOf(pattern, index - 1) : -1;
        }

        // Reverse the list to process from the end to the beginning
        matches.Reverse();

        // Create a dictionary of character indices
        var charIndices = token.Text
            .Select((c, i) => new { Char = c, Index = i })
            .ToDictionary(ci => ci.Index, ci => ci.Char);

        foreach (var hit in matches)
        {
            token.Text = token.Text.Remove(hit, patternLen).Insert(hit, replacementString);

            int charPosition = charIndices[hit];
            uint referenceOffset = token.ReferenceOffsets[charPosition];

            // Update the reference offsets
            token.ReferenceOffsets = token.ReferenceOffsets
                .Take(charPosition)
                .Concat(Enumerable.Repeat(referenceOffset, replacementCharLen))
                .Concat(token.ReferenceOffsets.Skip(charPosition + patternCharLen))
                .ToList();
        }
    }

    /// <summary>
    /// Split a text on special tokens (like BOS/EOS/UNK markers), depending on the vocabulary
    /// </summary>
    public static List<Token> SplitOnSpecialTokens(Token token, IVocab vocab)
    {
        List<Token> result = new List<Token>();

        int currentIndex = 0;
        while (currentIndex < token.Text.Length)
        {
            int matchLength = 0;
            int matchCharCount = 0;
            Mask matchMask = Mask.None;

            foreach (var specialValue in vocab.SpecialValues.Keys)
            {
                if (token.Text.Substring(currentIndex).StartsWith(specialValue))
                {
                    matchLength = specialValue.Length;
                    matchCharCount = specialValue.Length;
                    matchMask = vocab.GetUnknownValue() == specialValue ? Mask.Unknown : Mask.Special;
                    break;
                }
            }

            if (matchLength > 0)
            {
                // Create a new token for the matched special token
                var specialTokenText = token.Text.Substring(currentIndex, matchLength);
                var specialTokenOffsets = token.ReferenceOffsets.Skip(currentIndex).Take(matchCharCount).ToArray();
                result.Add(new Token(specialTokenText, specialTokenOffsets) { Mask = matchMask });
                currentIndex += matchLength;
            }
            else
            {
                // No special token found, move to the next character
                currentIndex++;
            }
        }

        return result;
    }

    /// <summary>
    /// Tokenizes CJK characters, each character will be a token
    /// </summary>
    public static List<Token> TokenizeCjkChars(Token token)
    {
        return SplitOnChar(token, IsCjkChar, true, Mask.CJK);
    }

    private static bool IsCjkChar(char character)
    {
        var u32Char = Convert.ToUInt32(character);
        return (0x4E00 <= u32Char && u32Char <= 0x9FFF)
            || (0x3400 <= u32Char && u32Char <= 0x4DBF)
            || (0x20000 <= u32Char && u32Char <= 0x2A6DF)
            || (0x2A700 <= u32Char && u32Char <= 0x2B73F)
            || (0x2B740 <= u32Char && u32Char <= 0x2B81F)
            || (0x2B820 <= u32Char && u32Char <= 0x2CEAF)
            || (0xF900 <= u32Char && u32Char <= 0xFAFF)
            || (0x2F800 <= u32Char && u32Char <= 0x2FA1F);
    }

    public static bool IsWhitespace(char character)
    {
        return Constants.WhitespaceChars.Contains(Convert.ToUInt32(character));
    }

    public static bool IsWhitespace(Rune character)
    {
        return Constants.WhitespaceChars.Contains(Convert.ToUInt32(character.Value));
    }

    /// <summary>
    /// This is a custom method to check if a character is a control character. The BERT tokenizer is
    /// taking any character whose unicode category starts with `C` as a control character, which includes
    /// the traditional control `Cc` category, but also the format `Cc`, private use `Co` and surrogate `Cs`.
    /// The unassigned unicode category `Cn` has been skipped in order to avoid unnecessary checks.
    /// A faster method may be called by setting strict to false and only check against the core control
    /// characters. To match the original BERT tokenization, this should remain true.
    /// </summary>
    public static bool IsControl(char character, bool strict)
    {
        if (Constants.AdditionalWhitespaceChars.Contains(character))
        {
            return false;
        }

        if (strict)
        {
            var u32Char = Convert.ToUInt32(character);
            return (u32Char <= 0x001F)
                || (0x0080 <= u32Char && u32Char <= 0x009F)
                || (0xE0020 <= u32Char && u32Char <= 0xE007F)
                || (0xE000 <= u32Char && u32Char <= 0xF8FF)
                || (0xF0000 <= u32Char && u32Char <= 0xFFFFD)
                || (0x100000 <= u32Char && u32Char <= 0x10FFFD)
                || (0xD800 <= u32Char && u32Char <= 0xDB7F)
                || (0xDB80 <= u32Char && u32Char <= 0xDBFF)
                || (0xDC00 <= u32Char && u32Char <= 0xDFFF)
                || Constants.ControlChars.Contains(u32Char);
        }
        else
        {
            return char.IsControl(character);
        }
    }

    public static bool IsControl(Rune character, bool strict)
    {
        if (Constants.AdditionalWhitespaceChars.Select(c => new Rune(c)).Contains(character))
        {
            return false;
        }

        if (strict)
        {
            var u32Char = Convert.ToUInt32(character.Value);
            return (u32Char <= 0x001F)
                || (0x0080 <= u32Char && u32Char <= 0x009F)
                || (0xE0020 <= u32Char && u32Char <= 0xE007F)
                || (0xE000 <= u32Char && u32Char <= 0xF8FF)
                || (0xF0000 <= u32Char && u32Char <= 0xFFFFD)
                || (0x100000 <= u32Char && u32Char <= 0x10FFFD)
                || (0xD800 <= u32Char && u32Char <= 0xDB7F)
                || (0xDB80 <= u32Char && u32Char <= 0xDBFF)
                || (0xDC00 <= u32Char && u32Char <= 0xDFFF)
                || Constants.ControlChars.Contains(u32Char);
        }
        else
        {
            return Rune.IsControl(character);
        }
    }

    public static bool IsPunctuation(char character)
    {
        var u32Char = Convert.ToUInt32(character);
        return (33 <= u32Char && u32Char <= 47)
            || (58 <= u32Char && u32Char <= 64)
            || (91 <= u32Char && u32Char <= 96)
            || (123 <= u32Char && u32Char <= 126)
            || Constants.PunctuationChars.Contains(u32Char);
    }

    /// <summary>
    /// Simple tokenization based on whitespace only
    /// </summary>
    public static List<Token> WhitespaceTokenize(Token token)
    {
        return SplitOnChar(token, IsWhitespace, false, Mask.Whitespace);
    }

    /// <summary>
    /// Lowercase
    /// </summary>
    public static void Lowercase(Token token)
    {
        var lowerCasedString = new StringBuilder(token.Text.Length);
        var characterMapping = new List<uint>(token.Text.Length);

        foreach (var (character, position) in token.Text.Zip(token.ReferenceOffsets))
        {
            foreach (var c in character.ToString().ToLower())
            {
                lowerCasedString.Append(c);
                characterMapping.Add(position);
            }
        }

        token.Text = lowerCasedString.ToString();
        token.ReferenceOffsets = characterMapping;
        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
    }

    /// <summary>
    /// Remove diacritics
    /// </summary>
    public static void StripAccents(Token token)
    {
        var decomposedString = new StringBuilder(token.Text.Length);
        var characterMapping = new List<uint>(token.Text.Length);

        foreach (var (character, position) in token.Text.Zip(token.ReferenceOffsets))
        {
            foreach (var c in character.ToString().Normalize(NormalizationForm.FormD))
            {
                if (!Constants.AccentMarkers.Contains(Convert.ToUInt32(c)))
                {
                    decomposedString.Append(c);
                    characterMapping.Add(position);
                }
            }
        }

        token.Text = decomposedString.ToString();
        token.ReferenceOffsets = characterMapping;
        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
    }

    /// <summary>
    /// NFKC decomposition
    /// </summary>
    public static void DecomposeNfkcV0(Token token)
    {
        var normalizedString = token.Text.Normalize(NormalizationForm.FormKC);
        var decomposedString = new StringBuilder(normalizedString.Length);
        var characterMapping = new List<uint>();

        int originalIndex = 0;
        foreach (var character in normalizedString)
        {
            decomposedString.Append(character);

            // Assuming each character in the normalized string maps to one character in the original string
            if (originalIndex < token.ReferenceOffsets.Count)
            {
                characterMapping.Add(token.ReferenceOffsets[originalIndex]);
            }
            else
            {
                // Handle cases where normalization adds characters
                characterMapping.Add(token.ReferenceOffsets.LastOrDefault());
            }

            originalIndex++;
        }

        token.Text = decomposedString.ToString();
        token.ReferenceOffsets = characterMapping;
        token.Offset = new Offset(token.ReferenceOffsets.FirstOrDefault(), token.ReferenceOffsets.LastOrDefault() + 1);
    }

    public static void DecomposeNfkc(Token token)
    {
        int capacity = Encoding.UTF8.GetByteCount(token.Text);
        StringBuilder decomposedString = new StringBuilder(capacity);
        List<uint> characterMapping = new List<uint>(capacity);
        int curPosition = 0;
        string normalizedString = token.Text.Normalize(NormalizationForm.FormKC);
        //var normalizedBytes = Encoding.UTF8.GetBytes(normalizedString, 0, normalizedString.Length);
        //var normalizedChars = Encoding.UTF8.GetChars(normalizedBytes);
        var itr = TokenizationUtils.CharIndicesForRunes(normalizedString);
        foreach (var (i, character) in itr)
        {
            var extraCharSize = 0;

            //if (Rune.IsLetterOrDigit(character) && !character.IsAscii && character.IsBmp && i != 0)
            //    extraCharSize = (character.Utf16SequenceLength - character.Utf8SequenceLength);

            // check if character is removed from the original text after normalization
            if (!token.Text.EnumerateRunes().Contains(character))
                extraCharSize = -1;

            decomposedString.Append(character);
            if (extraCharSize > 0)
            {
                curPosition -= extraCharSize;
            }
            // Assuming each character in the normalized string maps to one character in the original string
            if (curPosition < token.ReferenceOffsets.Count)
            {
                characterMapping.Add(token.ReferenceOffsets[curPosition]);
            }
            else
            {
                // Handle cases where normalization adds characters
                characterMapping.Add(token.ReferenceOffsets.LastOrDefault());
            }
            if (extraCharSize < 0)
            {
                curPosition -= extraCharSize;
            }
            curPosition += 1; // Adjust based on Unicode character width if needed
        }

        token.Text = decomposedString.ToString();//.Normalize(NormalizationForm.FormKC);
        token.ReferenceOffsets = characterMapping;
        token.Offset.Begin = token.ReferenceOffsets.FirstOrDefault();
        token.Offset.End = token.ReferenceOffsets.LastOrDefault() + 1;
    }

    public static void DecomposeNfkcV1(Token token)
    {
        int capacity = token.Text.Length;
        StringBuilder decomposedString = new StringBuilder(capacity);
        List<uint> characterMapping = new List<uint>(capacity);
        int curPosition = 0;

        foreach (char character in token.Text/*.Normalize(NormalizationForm.FormC)*/)
        {
            decomposedString.Append(character);
            characterMapping.Add(token.ReferenceOffsets[curPosition]);
            curPosition += 1; // Adjust based on Unicode character width if needed
        }

        token.Text = decomposedString.ToString().Normalize(NormalizationForm.FormKC);
        token.ReferenceOffsets = characterMapping;
        token.Offset.Begin = token.ReferenceOffsets.FirstOrDefault();
        token.Offset.End = token.ReferenceOffsets.LastOrDefault() + 1;
    }

    /// <summary>
    /// Split a token on punctuation
    /// </summary>
    public static List<Token> SplitOnPunct(Token token)
    {
        return SplitOnChar(token, IsPunctuation, true, Mask.Punctuation);
    }

    /// <summary>
    /// Split a token on one or more characters (given a character test function)
    /// </summary>
    public static List<Token> SplitOnChar(Token token, Func<char, bool> testCharacter, bool addSeparators, Mask setMask)
    {
        var tokens = new List<Token>();
        var charBegin = 0;
        var bytesBegin = 0;
        var charCount = 0;

        if (token.Mask == Mask.None)
        {
            for (int charIdx = 0; charIdx < token.Text.Length; charIdx++)
            {
                char c = token.Text[charIdx];
                int bytesIdx = Encoding.UTF8.GetByteCount(token.Text.Substring(0, charIdx));
                charCount += 1;

                if (testCharacter(c))
                {
                    if (charBegin < charIdx)
                    {
                        tokens.Add(new Token(
                            token.Text.Substring(bytesBegin, bytesIdx - bytesBegin),
                            new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)charIdx),
                            token.ReferenceOffsets.Skip(charBegin).Take(charIdx - charBegin).ToList(),
                            Mask.None
                        ));
                    }
                    if (addSeparators)
                    {
                        tokens.Add(new Token(
                            c.ToString(),
                            new Offset(token.Offset.Begin + (uint)charIdx, token.Offset.Begin + (uint)charIdx + 1),
                            new List<uint> { token.ReferenceOffsets[charIdx] },
                            setMask
                        ));
                    }
                    charBegin = charIdx + 1;
                    bytesBegin = bytesIdx + Encoding.UTF8.GetByteCount(c.ToString());
                }
            }
        }

        if (charBegin < token.Text.Length)
        {
            tokens.Add(new Token(
                token.Text.Substring(bytesBegin),
                new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)token.Text.Length),
                token.ReferenceOffsets.Skip(charBegin).ToList(),
                Mask.None
            ));
        }

        return tokens;
    }

    public static List<Token> SplitOnRegexWithLookahead(Token token, Regex patternLookahead, Regex patternTokenization)
    {
        if (token.Mask == Mask.None)
        {
            var subWords = new List<string>();
            var splits = new List<string>();

            int i = 0;
            foreach (Match hit in patternLookahead.Matches(token.Text))
            {
                var hitChars = hit.Value.Reverse().ToArray();
                var start = hitChars[0];
                var sep = hitChars[1];
                int endByte = hit.Index + hit.Length - sep.ToString().Length - start.ToString().Length;
                splits.Add(token.Text.Substring(i, endByte - i));
                i = endByte;
            }
            splits.Add(token.Text.Substring(i));

            foreach (var subWord in splits)
            {
                foreach (Match hit in patternTokenization.Matches(subWord))
                {
                    subWords.Add(hit.Value);
                }
            }

            var outputTokens = new List<Token>(subWords.Count);
            int beginChar = 0;
            foreach (var subWord in subWords)
            {
                int endChar = beginChar + subWord.Length;
                outputTokens.Add(new Token(
                    subWord,
                    new Offset(token.Offset.Begin + (uint)beginChar, token.Offset.Begin + (uint)endChar),
                    token.ReferenceOffsets.Skip(beginChar).Take(endChar - beginChar).ToArray(),
                    Mask.None
                ));
                beginChar = endChar;
            }

            return outputTokens;
        }
        else
        {
            return new List<Token> { token };
        }
    }


    public static List<Token> SplitOnRegex(Token token, Regex patternTokenization)
    {
        var tokens = new List<Token>();
        int beginChar = 0;

        foreach (Match hit in patternTokenization.Matches(token.Text))
        {
            int startByte = hit.Index;
            if (startByte > 0)
            {
                beginChar = token.Text.Substring(0, startByte).Length;
            }
            int endChar = beginChar + hit.Value.Length;

            tokens.Add(new Token(
                hit.Value,
                new Offset(token.Offset.Begin + (uint)beginChar, token.Offset.Begin + (uint)endChar),
                token.ReferenceOffsets.Skip(beginChar).Take(endChar - beginChar).ToArray(),
                Mask.None
            ));

            beginChar = endChar;
        }

        return tokens;
    }


    public static List<Token> SplitAtRegex(Token token, Regex patternTokenization)
    {
        var tokens = new List<Token>();
        int beginChar = 0;
        int startByte = 0;

        foreach (Match hit in patternTokenization.Matches(token.Text))
        {
            int hitStartByte = hit.Index;
            int hitStartChar = token.Text.Substring(0, hitStartByte).Length;
            int hitEndByte = hit.Index + hit.Length;
            int hitEndChar = beginChar + hit.Value.Length;

            if (!string.IsNullOrWhiteSpace(token.Text.Substring(startByte, hitStartByte - startByte)))
            {
                tokens.Add(new Token(
                    token.Text.Substring(startByte, hitStartByte - startByte),
                    new Offset(token.Offset.Begin + (uint)beginChar, token.Offset.Begin + (uint)hitStartChar),
                    token.ReferenceOffsets.Skip(beginChar).Take(hitStartChar - beginChar).ToArray(),
                    Mask.None
                ));
            }

            tokens.Add(new Token(
                hit.Value,
                new Offset(token.Offset.Begin + (uint)hitStartChar, token.Offset.Begin + (uint)hitEndChar),
                token.ReferenceOffsets.Skip(hitStartChar).Take(hitEndChar - hitStartChar).ToArray(),
                Mask.None
            ));

            beginChar = hitEndChar;
            startByte = hitEndByte;
        }

        if (!string.IsNullOrWhiteSpace(token.Text.Substring(startByte)))
        {
            tokens.Add(new Token(
                token.Text.Substring(startByte),
                new Offset(token.Offset.Begin + (uint)beginChar, (uint)token.Text.Length),
                token.ReferenceOffsets.Skip(startByte).ToArray(),
                Mask.None
            ));
        }

        return tokens;
    }


    /// <summary>
    /// Split a token on one or more substrings (given a substring test function)
    /// * token: The token to split
    /// * test_str: A function that contains the string buffer from the current point forward and
    /// returns a 3-tuple with the length of the match in bytes, chars and the mask to set (if the
    /// length is zero then there is no match.
    /// * add_separators: Add the separating characters to the tokens as well? (bool), separating tokens
    /// will be indicated in the returned mask by the value set in `set_mask`, which is returned by the test_substr function
    /// </summary>
    public static List<Token> SplitOnSubstr(Token token, Func<string, (int, int, Mask)> testSubstr, bool addSeparators)
    {
        var tokens = new List<Token>();
        int charBegin = 0;
        int bytesBegin = 0;
        int charCount = 0;

        if (token.Mask == Mask.None)
        {
            for (int charIdx = 0; charIdx < token.Text.Length; charIdx++)
            {
                charCount += 1;
                var (matchedBytes, matchedChars, setMask) = testSubstr(token.Text.Substring(charIdx));
                if (matchedChars > 0)
                {
                    if (charBegin < charIdx)
                    {
                        var text = token.Text.Substring(bytesBegin, charIdx - bytesBegin).TrimEnd();
                        var trimmedTextLen = text.Length;
                        if (trimmedTextLen > 0)
                        {
                            tokens.Add(new Token(
                                text,
                                new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)(charBegin + trimmedTextLen)),
                                token.ReferenceOffsets.Skip(charBegin).Take(trimmedTextLen).ToArray(),
                                Mask.None
                            ));
                        }
                    }
                    if (addSeparators)
                    {
                        tokens.Add(new Token(
                            token.Text.Substring(charIdx, matchedBytes),
                            new Offset(token.Offset.Begin + (uint)charIdx, token.Offset.Begin + (uint)(charIdx + matchedChars)),
                            token.ReferenceOffsets.Skip(charIdx).Take(matchedChars).ToArray(),
                            setMask
                        ));
                    }
                    charBegin = charIdx + matchedChars;
                    bytesBegin = charIdx + matchedBytes;
                }
            }
        }
        if (bytesBegin < token.Text.Length)
        {
            var remainingText = token.Text.Substring(bytesBegin);
            tokens.Add(new Token(
                remainingText,
                new Offset(token.Offset.Begin + (uint)charBegin, token.Offset.Begin + (uint)token.Text.Length),
                token.ReferenceOffsets.Skip(charBegin).ToArray(),
                Mask.None
            ));
        }
        return tokens;
    }

    /// <summary>
    /// Tokenize a token into word pieces according to the supplied vocabulary
    /// Continuation word pieces will all have the suffix `##`
    /// </summary>
    public static List<Token> TokenizeWordpiece(Token token, IVocab vocab, int maxWordLen)
    {
        var tokens = new List<Token>();
        if (token.Text.Length > maxWordLen)
        {
            tokens.Add(new Token(
                vocab.GetUnknownValue(),
                token.Offset,
                token.ReferenceOffsets.ToArray(),
                Mask.Unknown
            ));
        }
        else
        {
            var charIndices = token.Text.Select((c, i) => i).ToList();
            var maxEnd = charIndices.Last() + token.Text.Last().ToString().Length;
            var start = 0;
            var posBegin = 0;
            var posEnd = 0;
            var end = 0;

            while (start < maxEnd)
            {
                end = maxEnd;
                posEnd = charIndices.Count;
                var isUnk = true;

                while (start < end)
                {
                    var substr = token.Text.Substring(start, end - start);
                    var charLength = substr.Length;
                    var subOffset = new Offset(token.Offset.Begin + (uint)posBegin, token.Offset.Begin + (uint)(posBegin + charLength));

                    if (start > 0)
                    {
                        substr = "##" + substr;
                    }

                    if (vocab.Values.ContainsKey(substr))
                    {
                        tokens.Add(new Token(
                            substr,
                            subOffset,
                            token.ReferenceOffsets.Skip(posBegin).Take(charLength).ToArray(),
                            start > 0 ? Mask.Continuation : token.Mask
                        ));
                        isUnk = false;
                        break;
                    }

                    posEnd -= 1;
                    end = charIndices[posEnd];
                }

                if (isUnk)
                {
                    return new List<Token>
                {
                    new Token(
                        vocab.GetUnknownValue(),
                        token.Offset,
                        token.ReferenceOffsets.ToArray(),
                        Mask.Unknown
                    )
                };
                }

                start = end;
                posBegin = posEnd;
            }

            FixMask(tokens);
        }

        return tokens;
    }

    /// <summary>
    /// Truncates a sequence pair in place to the maximum length.
    /// </summary>
    /// <param name="tokenIdsWithOffsets1">First list of tokenized input ids.</param>
    /// <param name="tokenIdsWithOffsets2">Optional second list of input ids.</param>
    /// <param name="numTokensToRemove">Number of tokens to remove using the truncation strategy.</param>
    /// <param name="truncationStrategy">Truncation strategy.</param>
    /// <param name="stride">If set along with max_length, the overflowing tokens returned will contain some tokens from the main sequence returned.</param>
    /// <returns>Tuple of updated sequences, overflow tokens, and overflow offsets.</returns>
    /// <exception cref="TokenizerError">Thrown when truncation is not possible as per the provided parameters and strategy.</exception>

    public static (TokenIdsWithOffsets, TokenIdsWithOffsets?, List<long>, List<Offset?>) TruncateSequences(
        TokenIdsWithOffsets tokenIdsWithOffsets1,
        TokenIdsWithOffsets? tokenIdsWithOffsets2,
        int numTokensToRemove,
        TruncationStrategy truncationStrategy,
        int stride)
    {
        if (numTokensToRemove == 0)
        {
            return (tokenIdsWithOffsets1, tokenIdsWithOffsets2, new List<long>(), new List<Offset?>());
        }
        else if (tokenIdsWithOffsets2 != null)
        {
            switch (truncationStrategy)
            {
                case TruncationStrategy.LongestFirst:
                    if (tokenIdsWithOffsets1.Ids.Count + tokenIdsWithOffsets2.Ids.Count >= numTokensToRemove)
                    {
                        var overflowTokens = new List<long>(numTokensToRemove + stride);
                        var overflowOffsets = new List<Offset?>(numTokensToRemove + stride);
                        for (int i = 0; i < numTokensToRemove; i++)
                        {
                            if (tokenIdsWithOffsets1.Ids.Count >= tokenIdsWithOffsets2.Ids.Count)
                            {
                                overflowTokens.Insert(0, tokenIdsWithOffsets1.Ids.Last());
                                tokenIdsWithOffsets1.Ids.RemoveAt(tokenIdsWithOffsets1.Ids.Count - 1);
                                if (tokenIdsWithOffsets1.Offsets.Count > 0)
                                {
                                    overflowOffsets.Insert(0, tokenIdsWithOffsets1.Offsets.Last());
                                    tokenIdsWithOffsets1.Offsets.RemoveAt(tokenIdsWithOffsets1.Offsets.Count - 1);
                                }
                                tokenIdsWithOffsets1.ReferenceOffsets.RemoveAt(tokenIdsWithOffsets1.ReferenceOffsets.Count - 1);
                                if (tokenIdsWithOffsets1.Masks.Count > 0)
                                {
                                    tokenIdsWithOffsets1.Masks.RemoveAt(tokenIdsWithOffsets1.Masks.Count - 1);
                                }
                            }
                            else
                            {
                                overflowTokens.Insert(0, tokenIdsWithOffsets2.Ids.Last());
                                tokenIdsWithOffsets2.Ids.RemoveAt(tokenIdsWithOffsets2.Ids.Count - 1);
                                if (tokenIdsWithOffsets2.Offsets.Count > 0)
                                {
                                    overflowOffsets.Insert(0, tokenIdsWithOffsets2.Offsets.Last());
                                    tokenIdsWithOffsets2.Offsets.RemoveAt(tokenIdsWithOffsets2.Offsets.Count - 1);
                                }
                                tokenIdsWithOffsets2.ReferenceOffsets.RemoveAt(tokenIdsWithOffsets2.ReferenceOffsets.Count - 1);
                                if (tokenIdsWithOffsets2.Masks.Count > 0)
                                {
                                    tokenIdsWithOffsets2.Masks.RemoveAt(tokenIdsWithOffsets2.Masks.Count - 1);
                                }
                            }
                        }
                        // Handle stride
                        int windowLen = Math.Min(tokenIdsWithOffsets1.Ids.Count, stride);
                        if (windowLen > 0)
                        {
                            overflowTokens.InsertRange(0, tokenIdsWithOffsets1.Ids.GetRange(tokenIdsWithOffsets1.Ids.Count - windowLen, windowLen));
                            if (tokenIdsWithOffsets1.Offsets.Count > 0)
                            {
                                overflowOffsets.InsertRange(0, tokenIdsWithOffsets1.Offsets.GetRange(tokenIdsWithOffsets1.Offsets.Count - windowLen, windowLen));
                            }
                        }
                        return (tokenIdsWithOffsets1, tokenIdsWithOffsets2, overflowTokens, overflowOffsets);
                    }
                    else
                    {
                        throw new ValueTokenizerException("Combined sequence length too short for requested truncation amount");
                    }

                case TruncationStrategy.OnlyFirst:
                    if (tokenIdsWithOffsets1.Ids.Count >= numTokensToRemove)
                    {
                        var overflowTokens = new List<long>();
                        var overflowOffsets = new List<Offset?>();

                        // Truncate the first sequence
                        for (int i = 0; i < numTokensToRemove; i++)
                        {
                            overflowTokens.Insert(0, tokenIdsWithOffsets1.Ids.Last());
                            tokenIdsWithOffsets1.Ids.RemoveAt(tokenIdsWithOffsets1.Ids.Count - 1);

                            if (tokenIdsWithOffsets1.Offsets.Count > 0)
                            {
                                overflowOffsets.Insert(0, tokenIdsWithOffsets1.Offsets.Last());
                                tokenIdsWithOffsets1.Offsets.RemoveAt(tokenIdsWithOffsets1.Offsets.Count - 1);
                            }

                            tokenIdsWithOffsets1.ReferenceOffsets.RemoveAt(tokenIdsWithOffsets1.ReferenceOffsets.Count - 1);

                            if (tokenIdsWithOffsets1.Masks.Count > 0)
                            {
                                tokenIdsWithOffsets1.Masks.RemoveAt(tokenIdsWithOffsets1.Masks.Count - 1);
                            }
                        }

                        // Handle stride
                        int windowLen = Math.Min(tokenIdsWithOffsets1.Ids.Count, stride);
                        if (windowLen > 0)
                        {
                            overflowTokens.InsertRange(0, tokenIdsWithOffsets1.Ids.GetRange(tokenIdsWithOffsets1.Ids.Count - windowLen, windowLen));
                            if (tokenIdsWithOffsets1.Offsets.Count > 0)
                            {
                                overflowOffsets.InsertRange(0, tokenIdsWithOffsets1.Offsets.GetRange(tokenIdsWithOffsets1.Offsets.Count - windowLen, windowLen));
                            }
                        }

                        return (tokenIdsWithOffsets1, tokenIdsWithOffsets2, overflowTokens, overflowOffsets);
                    }
                    else
                    {
                        throw new ValueTokenizerException("First sequence too short for first only truncation");
                    }

                case TruncationStrategy.OnlySecond:
                    if (tokenIdsWithOffsets2.Ids.Count >= numTokensToRemove)
                    {
                        var overflowTokens = new List<long>(numTokensToRemove + stride);
                        var overflowOffsets = new List<Offset?>(numTokensToRemove + stride);

                        for (int i = 0; i < numTokensToRemove; i++)
                        {
                            overflowTokens.Insert(0, tokenIdsWithOffsets2.Ids.Last());
                            tokenIdsWithOffsets2.Ids.RemoveAt(tokenIdsWithOffsets2.Ids.Count - 1);

                            if (tokenIdsWithOffsets2.Offsets.Count > 0)
                            {
                                overflowOffsets.Insert(0, tokenIdsWithOffsets2.Offsets.Last());
                                tokenIdsWithOffsets2.Offsets.RemoveAt(tokenIdsWithOffsets2.Offsets.Count - 1);
                            }

                            tokenIdsWithOffsets2.ReferenceOffsets.RemoveAt(tokenIdsWithOffsets2.ReferenceOffsets.Count - 1);

                            if (tokenIdsWithOffsets2.Masks.Count > 0)
                            {
                                tokenIdsWithOffsets2.Masks.RemoveAt(tokenIdsWithOffsets2.Masks.Count - 1);
                            }
                        }

                        // Handle stride
                        int windowLen = Math.Min(tokenIdsWithOffsets2.Ids.Count, stride);
                        if (windowLen > 0)
                        {
                            overflowTokens.InsertRange(0, tokenIdsWithOffsets2.Ids.GetRange(tokenIdsWithOffsets2.Ids.Count - windowLen, windowLen));
                            if (tokenIdsWithOffsets2.Offsets.Count > 0)
                            {
                                overflowOffsets.InsertRange(0, tokenIdsWithOffsets2.Offsets.GetRange(tokenIdsWithOffsets2.Offsets.Count - windowLen, windowLen));
                            }
                        }

                        return (tokenIdsWithOffsets1, tokenIdsWithOffsets2, overflowTokens, overflowOffsets);
                    }
                    else
                    {
                        throw new ValueTokenizerException("Second sequence too short for second only truncation");
                    }

                case TruncationStrategy.DoNotTruncate:
                    throw new Exception("Truncation needed but no truncation requested");

                default:
                    throw new ArgumentOutOfRangeException(nameof(truncationStrategy), "Invalid truncation strategy");
            }
        }
        else if (tokenIdsWithOffsets1.Ids.Count >= numTokensToRemove)
        {
            switch (truncationStrategy)
            {
                case TruncationStrategy.LongestFirst:
                case TruncationStrategy.OnlyFirst:
                    var overflowTokens = new List<long>();
                    var overflowOffsets = new List<Offset?>();

                    // Truncate the first sequence
                    for (int i = 0; i < numTokensToRemove; i++)
                    {
                        overflowTokens.Insert(0, tokenIdsWithOffsets1.Ids.Last());
                        tokenIdsWithOffsets1.Ids.RemoveAt(tokenIdsWithOffsets1.Ids.Count - 1);

                        if (tokenIdsWithOffsets1.Offsets.Count > 0)
                        {
                            overflowOffsets.Insert(0, tokenIdsWithOffsets1.Offsets.Last());
                            tokenIdsWithOffsets1.Offsets.RemoveAt(tokenIdsWithOffsets1.Offsets.Count - 1);
                        }

                        tokenIdsWithOffsets1.ReferenceOffsets.RemoveAt(tokenIdsWithOffsets1.ReferenceOffsets.Count - 1);

                        if (tokenIdsWithOffsets1.Masks.Count > 0)
                        {
                            tokenIdsWithOffsets1.Masks.RemoveAt(tokenIdsWithOffsets1.Masks.Count - 1);
                        }
                    }

                    // Handle stride
                    int windowLen = Math.Min(tokenIdsWithOffsets1.Ids.Count, stride);
                    if (windowLen > 0)
                    {
                        overflowTokens.InsertRange(0, tokenIdsWithOffsets1.Ids.GetRange(tokenIdsWithOffsets1.Ids.Count - windowLen, windowLen));
                        if (tokenIdsWithOffsets1.Offsets.Count > 0)
                        {
                            overflowOffsets.InsertRange(0, tokenIdsWithOffsets1.Offsets.GetRange(tokenIdsWithOffsets1.Offsets.Count - windowLen, windowLen));
                        }
                    }

                    return (tokenIdsWithOffsets1, tokenIdsWithOffsets2, overflowTokens, overflowOffsets);

                case TruncationStrategy.OnlySecond:
                    throw new ValueTokenizerException("Invalid truncation strategy for single sentence truncation");

                case TruncationStrategy.DoNotTruncate:
                    throw new ValueTokenizerException("Truncation needed but no truncation requested");

                default:
                    throw new ArgumentOutOfRangeException(nameof(truncationStrategy), "Invalid truncation strategy");
            }
        }
        else
        {
            throw new ValueTokenizerException("First sequence too short for first only truncation");
        }
    }

    public static (List<long>, List<Offset?>) TruncateWithOverflow(
        List<long> sequence,
        List<Offset?> offsets,
        List<List<uint>> originalPositions,
        List<Mask> mask,
        int numTokensToRemove,
        int stride)
    {
        if (offsets.Any())
        {
            if (sequence.Count != offsets.Count)
                throw new ArgumentException("Sequence and offsets must be of the same length.");
        }
        if (mask.Any())
        {
            if (sequence.Count != mask.Count)
                throw new ArgumentException("Sequence and mask must be of the same length.");
        }

        int cutoff = sequence.Count - numTokensToRemove;
        List<long> overflowTokens = sequence.GetRange(cutoff, numTokensToRemove);
        sequence.RemoveRange(cutoff, numTokensToRemove);

        List<Offset?> overflowOffsets = new List<Offset?>();
        if (offsets.Any())
        {
            overflowOffsets = offsets.GetRange(cutoff, offsets.Count - cutoff);
            offsets.RemoveRange(cutoff, offsets.Count - cutoff);
        }

        if (mask.Any())
        {
            mask.RemoveRange(cutoff, mask.Count - cutoff);
            originalPositions.RemoveRange(cutoff, originalPositions.Count - cutoff);
        }

        int windowLen = Math.Min(sequence.Count, stride);
        if (windowLen > 0)
        {
            overflowTokens.InsertRange(0, sequence.GetRange(sequence.Count - windowLen, windowLen));
            if (offsets.Any())
            {
                overflowOffsets.InsertRange(0, offsets.GetRange(offsets.Count - windowLen, windowLen));
            }
        }

        return (overflowTokens, overflowOffsets);
    }

    public static HashSet<(string, string)> GetPairs(List<string> tokens)
    {
        if (tokens.Count < 2)
        {
            return null;
        }

        var output = new HashSet<(string, string)>();
        for (int idx = 0; idx < tokens.Count - 1; idx++)
        {
            output.Add((tokens[idx], tokens[idx + 1]));
        }

        return output;
    }

    public static (List<string>, bool) GroupCommonPairs(List<string> tokens, Dictionary<(string, string), long> bpeRanks)
    {
        var pairs = GetPairs(tokens);
        if (pairs == null || !pairs.Any())
        {
            return (tokens, true);
        }

        var bigram = pairs
            .OrderBy(pair => bpeRanks.TryGetValue(pair, out long rank) ? rank : long.MaxValue)
            .FirstOrDefault();

        if (!bpeRanks.ContainsKey(bigram))
        {
            return (tokens, true);
        }

        var tempSubTokens = new List<string>();
        int i = 0;

        while (i < tokens.Count)
        {
            int j = tokens.FindIndex(i, t => t == bigram.Item1);
            if (j == -1)
            {
                tempSubTokens.AddRange(tokens.GetRange(i, tokens.Count - i));
                break;
            }

            tempSubTokens.AddRange(tokens.GetRange(i, j - i));
            i = j;

            if (tokens[i] == bigram.Item1 && i < tokens.Count - 1)
            {
                if (tokens[i + 1] == bigram.Item2)
                {
                    string combinedBytes = bigram.Item1 + bigram.Item2;
                    tempSubTokens.Add(combinedBytes);
                    i += 2;
                }
                else
                {
                    tempSubTokens.Add(bigram.Item1);
                    i++;
                }
            }
            else
            {
                tempSubTokens.Add(bigram.Item1);
                i++;
            }
        }

        return (tempSubTokens, tempSubTokens.Count == 1);
    }

    public static (List<string>, List<int>) CtrlBpe(string token, Dictionary<(string, string), long> bpeRanks)
    {
        var subTokens = token.ToCharArray().Select(c => c.ToString()).ToList();
        if (subTokens.Any())
        {
            subTokens[subTokens.Count - 1] += "</w>";
        }

        var output = (subTokens, false);
        while (true)
        {
            output = GroupCommonPairs(output.Item1, bpeRanks);
            if (output.Item2)
            {
                break;
            }
        }

        int length = output.Item1.Count;
        for (int i = 0; i < length; i++)
        {
            if (i < length - 1)
            {
                output.Item1[i] += "@@";
            }
            else
            {
                output.Item1[i] = output.Item1[i].TrimEnd("</w>".ToCharArray());
            }
        }

        var charCounts = output.Item1.Select(v => v.TrimEnd("@@".ToCharArray()).Length).ToList();
        return (output.Item1, charCounts);
    }


    public static (List<string>, List<int>) OpenAIGptBpe(string token, Dictionary<(string, string), long> bpeRanks)
    {
        var subTokens = token.ToCharArray().Select(c => c.ToString()).ToList();
        if (subTokens.Any())
        {
            subTokens[subTokens.Count - 1] += "</w>";
        }

        var output = (subTokens, false);
        while (true)
        {
            output = GroupCommonPairs(output.Item1, bpeRanks);
            if (output.Item2)
            {
                break;
            }
        }

        var charCounts = output.Item1.Select(v => v.TrimEnd("</w>".ToCharArray()).Length).ToList();
        return (output.Item1, charCounts);
    }


    public static (List<string>, List<int>) Bpe(string token, Dictionary<(string, string), long> bpeRanks)
    {
        var subTokens = token.ToCharArray().Select(c => c.ToString()).ToList();

        var output = (subTokens, false);
        while (true)
        {
            output = GroupCommonPairs(output.Item1, bpeRanks);
            if (output.Item2)
            {
                break;
            }
        }

        var charCounts = output.Item1.Select(v => v.Length).ToList();
        return (output.Item1, charCounts);
    }

    public static List<int> BytesOffsets(string text)
    {
        var offsets = new List<int>(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            var character = text[i];
            int charBytes = Char.IsSurrogate(character) ? 4 : 2;
            for (int j = 0; j < charBytes; j++)
            {
                offsets.Add(i);
            }
        }
        return offsets;
    }

    public static List<Token> SplitOnBpePairs(
        Token token,
        Func<string, Dictionary<(string, string), long>, (List<string>, List<int>)> bpeFunction,
        Dictionary<(string, string), long> bpeRanks,
        Dictionary<string, (List<string>, List<int>)> cache,
        bool asBytes)
    {
        var tokens = new List<Token>();
        string textToProcess;
        List<uint> referenceOffsets;

        if (asBytes)
        {
            // Convert the string to bytes and then map each byte to a character
            var bytes = Encoding.UTF8.GetBytes(token.Text);
            var referenceOffsetsPlaceholder = BytesOffsets(token.Text)
                .Select(pos => token.ReferenceOffsets[pos])
                .ToList();
            textToProcess = new string(bytes.Select(b => Constants.BytesToUnicode[b]).ToArray());
            referenceOffsets = referenceOffsetsPlaceholder;
        }
        else
        {
            textToProcess = token.Text;
            referenceOffsets = token.ReferenceOffsets.ToList();
        }

        bool cached = false;
        if (cache.TryGetValue(textToProcess, out var cacheValue))
        {
            var (cachedTokens, charCounts) = cacheValue;
            int start = 0;
            for (int idx = 0; idx < cachedTokens.Count; idx++)
            {
                var subToken = cachedTokens[idx];
                var charCount = charCounts[idx];
                tokens.Add(new Token(
                    subToken,
                    new Offset(referenceOffsets[start], referenceOffsets[start + charCount - 1] + 1),
                    referenceOffsets.GetRange(start, charCount),
                    cachedTokens.Count > 1 ? (idx == 0 ? Mask.Begin : Mask.Continuation) : Mask.None
                ));
                start += charCount;
            }
            cached = true;
        }

        if (!cached)
        {
            var (bpeOutput, charCounts) = bpeFunction(textToProcess, bpeRanks);
            cache[textToProcess] = (bpeOutput, charCounts);
            int start = 0;
            for (int idx = 0; idx < bpeOutput.Count; idx++)
            {
                var subToken = bpeOutput[idx];
                var charCount = charCounts[idx];
                tokens.Add(new Token(
                    subToken,
                    new Offset(referenceOffsets[start], referenceOffsets[start + charCount - 1] + 1),
                    referenceOffsets.GetRange(start, charCount),
                    bpeOutput.Count > 1 ? (idx == 0 ? Mask.Begin : Mask.Continuation) : Mask.None
                ));
                start += charCount;
            }
        }
        return tokens;
    }


    private static void FixMask(List<Token> tokens)
    {
        for (var i = 1; i < tokens.Count; i++)
        {
            if (tokens[i].Mask == Mask.Continuation && tokens[i - 1].Mask == Mask.None)
            {
                tokens[i - 1].Mask = Mask.Begin;
            }
        }
    }

    public static List<Token> SplitOnLanguageCode(Token token, int codeLength, HashSet<byte[]> languageCodesBytes)
    {
        if (Encoding.UTF8.GetByteCount(token.Text) < codeLength)
        {
            return new List<Token> { token };
        }

        var tokens = new List<Token>();
        int beginChar = 0;
        int startByte = 0;

        // Skip leading whitespace
        foreach (var c in token.Text)
        {
            if (!char.IsWhiteSpace(c))
            {
                break;
            }
            startByte += Encoding.UTF8.GetByteCount(new char[] { c });
            beginChar++;
        }

        var leadingBytes = Encoding.UTF8.GetBytes(token.Text).Skip(startByte).Take(codeLength).ToArray();
        if (languageCodesBytes.Contains(leadingBytes))
        {
            tokens.Add(new Token(
                token.Text.Substring(startByte, codeLength),
                new Offset((uint)token.Offset.Begin + (uint)beginChar, (uint)token.Offset.Begin + (uint)beginChar + (uint)codeLength),
                token.ReferenceOffsets.Skip(beginChar).Take(codeLength).ToArray(),
                Mask.Special
            ));

            startByte += codeLength;
            beginChar += Encoding.UTF8.GetChars(leadingBytes).Length;

            // Skip whitespace after language code
            foreach (var c in token.Text.Substring(startByte))
            {
                if (!char.IsWhiteSpace(c))
                {
                    break;
                }
                startByte += Encoding.UTF8.GetByteCount(new char[] { c });
                beginChar++;
            }
        }

        tokens.Add(new Token(
            token.Text.Substring(startByte),
            new Offset((uint)token.Offset.Begin + (uint)beginChar, (uint)token.Text.Length),
            token.ReferenceOffsets.Skip(beginChar).ToArray(),
            Mask.None
        ));

        return tokens;
    }

    public static List<Token> UnknownByteFallback(Token token, IVocab vocab)
    {
        if (!vocab.Values.ContainsKey(token.Text))
        {
            var updatedTokens = new List<Token>();
            foreach (var byteValue in Encoding.UTF8.GetBytes(token.Text))
            {
                var byteStr = $"<{byteValue:X2}>";
                updatedTokens.Add(new Token(
                    byteStr,
                    new Offset(token.Offset.End, token.Offset.End),
                    new uint[] { token.ReferenceOffsets.Last() },
                    token.Mask
                ));
            }
            return updatedTokens;
        }
        else
        {
            return null;
        }
    }
}
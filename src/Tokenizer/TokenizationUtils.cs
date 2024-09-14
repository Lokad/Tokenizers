using System.Text.RegularExpressions;
using System.Text;
using Lokad.Tokenizers.Vocab;
using System.Globalization;
using Lokad.Tokenizers.Exceptions;
using System.Runtime.CompilerServices;
using System.Buffers;

[assembly: InternalsVisibleToAttribute("Lokad.Tokenizers.Tests")]

namespace Lokad.Tokenizers.Tokenizer;

internal static class TokenizationUtils
{
    /// <summary>
    /// Substring Runes (characters)
    /// </summary>
    public static string SubstringRunes(string text, int start, int length)
    {
        var sb = new StringBuilder();
        text.EnumerateRunes().Skip(start).Take(length).ToList().ForEach(r => sb.Append(r));
        return sb.ToString();
    }

    /// <summary>
    /// Substring Runes (characters)
    /// </summary>
    public static string SubstringRunes(string text, int start)
    {
        var sb = new StringBuilder();
        text.EnumerateRunes().Skip(start).ToList().ForEach(r => sb.Append(r));
        return sb.ToString();
    }


    /// <summary>
    /// Get UTF 8 Bytes Count
    /// </summary>
    public static int GetUtf8BytesCount(string text)
    {
        return Encoding.UTF8.GetByteCount(text);
    }

    /// <summary>
    /// Get Runes with index
    /// </summary>
    public static IEnumerable<(int Index, Rune Character)> CharIndicesForRunes(string str)
    {
        var front_offset = 0;
        var runes = str.EnumerateRunes().ToList();
        for (var i = 0; i < runes.Count; i++)
        {
            var index = front_offset;
            yield return (index, runes[i]);
            front_offset += runes[i].Utf8SequenceLength;
        }
    }

    /// <summary>
    /// NFKC decomposition
    /// </summary>
    public static IEnumerable<(Rune Character, int ExtraCharSize)> NFKC(string str)
    {
        var runes = str.EnumerateRunes().ToList();
        for (var i = 0; i < runes.Count; i++)
        {
            yield return (runes[i], (runes[i].Utf8SequenceLength - runes[i].Utf16SequenceLength));
        }
    }

    /// <summary>
    /// Enumerate
    /// </summary>
    public static IEnumerable<(int Index, T Element)> Enumerate<T>(IEnumerable<T> source)
    {
        var index = 0;
        foreach (var element in source)
        {
            yield return (index++, element);
        }
    }

    /// <summary>
    /// Substring by byte offset
    /// </summary>
    public static string SubstringByByteOffset(string s, int start)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        var substringBytes = new byte[bytes.Length - start];
        Array.Copy(bytes, start, substringBytes, 0, bytes.Length - start);
        return Encoding.UTF8.GetString(substringBytes);
    }

    public static byte[] SubstringByByteOffset(byte[] bytes, int start)
    {
        var substringBytes = new byte[bytes.Length - start];
        Array.Copy(bytes, start, substringBytes, 0, bytes.Length - start);
        return substringBytes;
    }

    /// <summary>
    /// Substring by byte offset
    /// </summary>
    public static string SubstringByByteOffset(string s, int start, int end)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        if (end > bytes.Length || start > end)
        {
            throw new ArgumentOutOfRangeException("Invalid range");
        }
        var substringBytes = new byte[end - start];
        Array.Copy(bytes, start, substringBytes, 0, end - start);
        return Encoding.UTF8.GetString(substringBytes);
    }
    public static byte[] SubstringByByteOffset(byte[] bytes, int start, int end)
    {
        if (end > bytes.Length || start > end)
        {
            throw new ArgumentOutOfRangeException("Invalid range");
        }
        var substringBytes = new byte[end - start];
        Array.Copy(bytes, start, substringBytes, 0, end - start);
        return substringBytes;
    }

    /// <summary>
    /// Enumerate Runes by reading utf8 bytes until the end
    /// </summary>
    public static IEnumerable<Rune> BytesToRunes(byte[] bytes)
    {
        var index = 0;
        while (index < bytes.Length)
        {
            var remBytes = SubstringByByteOffset(bytes, index);
            var status = Rune.DecodeFromUtf8(remBytes, out var rune, out var bytesConsumed);
            if (status == OperationStatus.InvalidData)
            {
                throw new Exception("Invalid UTF-8 data");
            }
            index += bytesConsumed;
            yield return rune;
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
    /// Is Whitespace Rune ?
    /// </summary>
    public static bool IsWhitespace(Rune character)
    {
        return Constants.WhitespaceChars.Contains(Convert.ToUInt32(character.Value));
    }

    /// <summary>
    /// Is Control Rune?
    /// </summary>
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
    /// NFKC decomposition
    /// </summary>
    public static void DecomposeNfkc(Token token)
    {
        var capacity = Encoding.UTF8.GetByteCount(token.Text);
        var decomposedString = new StringBuilder(capacity);
        var characterMapping = new List<uint>(capacity);
        var curPosition = 0;
        var normalizedString = token.Text.Normalize(NormalizationForm.FormKC);
        foreach (var (character, currentExtraCharSize) in TokenizationUtils.NFKC(normalizedString))
        {
            var extraCharSize = 0;

            //HINT: [@eslam] check if character is removed from the original text after normalization
            if (!token.Text.EnumerateRunes().Contains(character))
                extraCharSize -= currentExtraCharSize;

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
                        for (var i = 0; i < numTokensToRemove; i++)
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
                        var windowLen = Math.Min(tokenIdsWithOffsets1.Ids.Count, stride);
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
                        for (var i = 0; i < numTokensToRemove; i++)
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
                        var windowLen = Math.Min(tokenIdsWithOffsets1.Ids.Count, stride);
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

                        for (var i = 0; i < numTokensToRemove; i++)
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
                        var windowLen = Math.Min(tokenIdsWithOffsets2.Ids.Count, stride);
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
                    for (var i = 0; i < numTokensToRemove; i++)
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
                    var windowLen = Math.Min(tokenIdsWithOffsets1.Ids.Count, stride);
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

}
using System.Text;
using Lokad.Tokenizers.Exceptions;
using Lokad.Tokenizers.Vocab;

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Whitespace, punctuation and CJK tokenization with scalar source offsets.</summary>
/// <remarks>Share instances only when the vocabulary is not being modified.</remarks>
public class BaseTokenizer<T> : ITokenizer<T> where T : IVocab
{
    private readonly T vocab;
    private readonly bool lowerCase;
    private readonly bool stripAccents;
    private readonly LiteralTokenMatcher specialTokens;
    private readonly IVocab literalVocabulary;

    /// <summary>Load a supplied BaseVocab flat file or XlmRobertaVocab protobuf and a special-token mapping.</summary>
    public BaseTokenizer(string path, bool lowerCase, bool stripAccents, string specialTokenMappingPath)
        : this(LoadVocabulary(path, specialTokenMappingPath), lowerCase, stripAccents) { }

    /// <summary>Load a supplied BaseVocab flat file or XlmRobertaVocab protobuf.</summary>
    public BaseTokenizer(string path, bool lowerCase, bool stripAccents)
        : this(LoadVocabulary(path, null), lowerCase, stripAccents) { }

    /// <summary>Use an already loaded vocabulary, including custom IVocab implementations.</summary>
    public BaseTokenizer(T vocab, bool lowerCase, bool stripAccents)
    {
        this.vocab = vocab ?? throw new ArgumentNullException(nameof(vocab));
        this.lowerCase = lowerCase;
        this.stripAccents = stripAccents;
        literalVocabulary = vocab;
        specialTokens = new LiteralTokenMatcher(vocab.SpecialValues.Select(p => (p.Key, p.Value)));
    }

    private static T LoadVocabulary(string path, string? mapping)
    {
        IVocab loaded;
        if (typeof(T) == typeof(BaseVocab))
            loaded = new BaseVocab(VocabHelper.ReadFlatFile(path),
                mapping == null ? new SpecialTokenMap() : VocabHelper.ReadSpecialTokenMappingFile(mapping));
        else if (typeof(T) == typeof(XlmRobertaVocab))
            loaded = mapping == null ? XlmRobertaVocab.FromFile(path) : XlmRobertaVocab.FromFileWithSpecialTokenMapping(path, mapping);
        else throw new NotSupportedException("Construct a custom tokenizer with an already loaded vocabulary");
        return (T)loaded;
    }

    public T Vocab => vocab;
    public virtual List<string> Tokenize(string text)
    {
        if (IsEmptyInput(text)) return [];
        // Unknown derived tokenizers retain their full-metadata virtual input contract.
        var token = new Token(text, trackOffsets: GetType() != typeof(BaseTokenizer<T>));
        return TokenizeToTokens(token).Select(t => t.Text).ToList();
    }

    public TokensWithOffsets TokenizeWithOffsets(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (IsEmptyInput(text)) return new();
        var result = new TokensWithOffsets();
        foreach (Token token in TokenizeToTokens(new Token(text)))
        {
            IReadOnlyList<uint> positions = TokenAlignment.SourcePositions(token);
            result.Tokens.Add(token.Text);
            result.Offsets.Add(positions.Count == 0 ? null : new Offset(positions.Min(), checked(positions.Max() + 1)));
            result.ReferenceOffsets.Add(positions);
            result.Masks.Add(token.Mask);
        }
        return result;
    }

    public virtual List<Token> TokenizeToTokens(Token initialToken)
    {
        ArgumentNullException.ThrowIfNull(initialToken);
        if (initialToken.Mask is Mask.Special or Mask.Unknown) UnicodeProfile.ValidateUtf16(initialToken.Text);
        if (initialToken.Mask is not Mask.Special and not Mask.Unknown && IsEmptyInput(initialToken.Text)) return [];
        var result = new List<Token>();
        // Recognize special tokens before cleaning or splitting their content.
        foreach (Token part in SplitOnSpecialTokens(initialToken))
        {
            if (part.Mask is Mask.Special or Mask.Unknown) { result.Add(part); continue; }
            TokenizationUtils.CleanText(part);
            if (lowerCase) TokenizationUtils.Lowercase(part);
            if (stripAccents) TokenizationUtils.StripAccents(part);
            foreach (Token word in Split(part, UnicodeProfile.IsWhiteSpace, null))
                foreach (Token punctuation in Split(word, UnicodeProfile.IsPunctuation, Mask.Punctuation))
                    if (punctuation.Mask == Mask.Punctuation) result.Add(punctuation);
                    else result.AddRange(Split(punctuation, IsCjk, Mask.CJK));
        }
        return result;
    }

    public void DecomposeNfkc(Token token) => TokenizationUtils.DecomposeNfkc(token);

    protected virtual bool IsEmptyInput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        UnicodeProfile.ValidateUtf16(text);
        return text.Length == 0 || UnicodeProfile.IsWhiteSpace(text) && !specialTokens.ContainsWhitespaceLiteral(text);
    }
    protected bool ContainsSpecialToken(string text) => specialTokens.ContainsMatch(text);
    public List<long> ConvertTokensToIds(List<string> tokens) => tokens.Select(vocab.TokenToId).ToList();

    public TokenizedInput Encode(string inputText, string? additionalInputText, int maxLen,
        TruncationStrategy truncationStrategy, int stride)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLen);
        ArgumentOutOfRangeException.ThrowIfNegative(stride);
        if (!Enum.IsDefined(truncationStrategy)) throw new ArgumentOutOfRangeException(nameof(truncationStrategy));
        int? knownSpecialCount = GetBuiltInSpecialCount(additionalInputText != null);
        void CheckMinimum(int count)
        {
            if (maxLen < count) throw new ValueTokenizerException("Maximum length is smaller than the required special-token template");
        }
        if (knownSpecialCount is int minimum) CheckMinimum(minimum);
        TokenIdsWithOffsets Convert(string text)
        {
            var result = new TokenIdsWithOffsets();
            if (IsEmptyInput(text)) return result;
            foreach (Token token in TokenizeToTokens(new Token(text)))
            {
                var positions = TokenAlignment.SourcePositionList(token);
                result.Ids.Add(vocab.TokenToId(token.Mask == Mask.Unknown ? vocab.GetUnknownValue() : token.Text));
                result.Offsets.Add(positions.Count == 0 ? null : new Offset(positions.Min(), checked(positions.Max() + 1)));
                result.ReferenceOffsets.Add(positions);
                result.Masks.Add(token.Mask);
            }
            return result;
        }
        var first = Convert(inputText);
        var second = additionalInputText == null ? null : Convert(additionalInputText);
        int specialCount = knownSpecialCount ?? BuildInputWithSpecialTokens(new(), second == null ? null : new()).TokenIds.Count;
        if (!knownSpecialCount.HasValue) CheckMinimum(specialCount);
        int remove = Math.Max(0, checked(first.Ids.Count + (second?.Ids.Count ?? 0) + specialCount) - maxLen);
        var (truncatedFirst, truncatedSecond, overflow, _) =
            TokenizationUtils.TruncateSequences(first, second, remove, truncationStrategy, stride, includeOverflowOffsets: false);
        var merged = knownSpecialCount.HasValue
            ? BuildInputCore(truncatedFirst, truncatedSecond, takeOwnership: true)
            : BuildInputWithSpecialTokens(truncatedFirst, truncatedSecond);
        if (!knownSpecialCount.HasValue && merged.TokenIds.Count !=
            truncatedFirst.Ids.Count + (truncatedSecond?.Ids.Count ?? 0) + specialCount)
            throw new InvalidOperationException("Derived templates must insert a content-independent number of tokens");
        if (merged.TokenIds.Count > maxLen)
            throw new ValueTokenizerException("The special-token template exceeds the maximum length");
        return new()
        {
            TokenIds = merged.TokenIds, SegmentIds = merged.SegmentIds,
            SpecialTokensMask = merged.SpecialTokensMask, OverflowingTokens = overflow,
            NumTruncatedTokens = remove, TokenOffsets = merged.TokenOffsets,
            ReferenceOffsets = merged.ReferenceOffsets, Mask = merged.Mask
        };
    }

    /// <summary>Combine sequences into owned lists and metadata, independently mutable from either input.</summary>
    /// <remarks>Derived templates must insert a content-independent number of tokens. Encode counts that overhead on empty sequences and validates the final count.</remarks>
    public virtual TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets inputTokens,
        TokenIdsWithOffsets? additionalInputTokens = null) => BuildInputCore(inputTokens, additionalInputTokens, takeOwnership: false);

    // Unknown derived classes keep the public virtual template hook and its defensive ownership boundary.
    internal virtual int? GetBuiltInSpecialCount(bool paired) => GetType() == typeof(BaseTokenizer<T>) ? 0 : null;

    internal virtual TokenIdsWithSpecialTokens BuildInputCore(TokenIdsWithOffsets inputTokens,
        TokenIdsWithOffsets? additionalInputTokens, bool takeOwnership)
    {
        var result = new TokenIdsWithSpecialTokens();
        result.Append(inputTokens, 0, takeOwnership);
        if (additionalInputTokens != null) result.Append(additionalInputTokens, 1, takeOwnership);
        return result;
    }

    public string Decode(List<long> tokenIds, bool skipSpecialTokens, bool cleanUpTokenizationSpaces)
    {
        ArgumentNullException.ThrowIfNull(tokenIds);
        var tokens = tokenIds.Where(id => !skipSpecialTokens || !vocab.SpecialIndices.ContainsKey(id))
            .Select(vocab.IdToToken).ToList();
        string text = ConvertTokensToString(tokens);
        return cleanUpTokenizationSpaces ? CleanUpTokenization(text) : text;
    }

    public virtual string ConvertTokensToString(List<string> tokens) => string.Join(" ", tokens);

    public virtual string CleanUpTokenization(string inputString) => inputString
        .Replace(" .", ".").Replace(" !", "!").Replace(" ?", "?").Replace(" ,", ",")
        .Replace(" ' ", "'").Replace(" n't", "n't").Replace(" 'm", "'m")
        .Replace(" 's", "'s").Replace(" 've", "'ve").Replace(" 're", "'re");

    /// <summary>Split using this tokenizer's configured, cached literal vocabulary.</summary>
    protected List<Token> SplitOnSpecialTokens(Token token) => SplitOnSpecialTokens(token, literalVocabulary, specialTokens);

    protected List<Token> SplitOnSpecialTokens(Token token, IVocab vocabulary)
    {
        if (token.Mask is Mask.Special or Mask.Unknown) return new() { token.Clone() };
        return SplitOnSpecialTokens(token, vocabulary, ReferenceEquals(vocabulary, literalVocabulary) ? specialTokens :
            new LiteralTokenMatcher(vocabulary.SpecialValues.Select(p => (p.Key, p.Value))));
    }

    private static List<Token> SplitOnSpecialTokens(Token token, IVocab vocabulary, LiteralTokenMatcher matcher)
    {
        if (token.Mask is Mask.Special or Mask.Unknown) return new() { token.Clone() };
        var matches = matcher.FindMatches(token.Text);
        var references = token.TracksOffsets ? TokenAlignment.References(token) : null;
        var result = new List<Token>();
        int position = 0, scalar = 0, start = 0, startScalar = 0;
        void Add(int end, int endScalar, Mask mask)
        {
            if (end == start) return;
            result.Add(TokenAlignment.Slice(token.Text[start..end], references, startScalar, endScalar - startScalar, mask));
        }
        while (position < token.Text.Length)
        {
            if (!matcher.TryMatch(matches, position, out var match))
            {
                position += Rune.GetRuneAt(token.Text, position).Utf16SequenceLength;
                scalar++;
                continue;
            }
            Add(position, scalar, Mask.None);
            start = position;
            startScalar = scalar;
            position += match.Text.Length;
            scalar += match.Text.EnumerateRunes().Count();
            Add(position, scalar, match.Text == vocabulary.GetUnknownValue() ? Mask.Unknown : Mask.Special);
            start = position;
            startScalar = scalar;
        }
        Add(position, scalar, Mask.None);
        return result;
    }

    private static List<Token> Split(Token token, Func<Rune, bool> isSeparator, Mask? separatorMask)
    {
        if (token.Mask is Mask.Special or Mask.Unknown) return new() { token.Clone() };
        if (token.Text.Length == 0) return [];
        var references = token.TracksOffsets ? TokenAlignment.References(token) : null;
        var result = new List<Token>();
        int start = 0, startUtf16 = 0, utf16 = 0;
        void Add(int begin, int end, int beginUtf16, int endUtf16, Mask mask)
        {
            if (begin == end) return;
            result.Add(TokenAlignment.Slice(token.Text[beginUtf16..endUtf16], references, begin, end - begin, mask));
        }
        int i = 0;
        foreach (Rune rune in token.Text.EnumerateRunes())
        {
            int next = utf16 + rune.Utf16SequenceLength;
            if (isSeparator(rune))
            {
                Add(start, i, startUtf16, utf16, token.Mask);
                if (separatorMask is { } mask) Add(i, i + 1, utf16, next, mask);
                start = i + 1;
                startUtf16 = next;
            }
            utf16 = next;
            i++;
        }
        if (start == 0) result.Add(token);
        else Add(start, i, startUtf16, utf16, token.Mask);
        return result;
    }

    private static bool IsCjk(Rune rune) => rune.Value is
        >= 0x3400 and <= 0x4DBF or >= 0x4E00 and <= 0x9FFF or >= 0xF900 and <= 0xFAFF or
        >= 0x20000 and <= 0x2EBEF or >= 0x30000 and <= 0x323AF or
        >= 0x3040 and <= 0x30FF or >= 0x31F0 and <= 0x31FF or
        >= 0x1100 and <= 0x11FF or >= 0x3130 and <= 0x318F or
        >= 0xA960 and <= 0xA97F or >= 0xAC00 and <= 0xD7FF;
}

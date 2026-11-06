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
    public List<string> Tokenize(string text) => TokenizeWithOffsets(text).Tokens;

    public TokensWithOffsets TokenizeWithOffsets(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text)) return new();
        var result = new TokensWithOffsets();
        foreach (Token token in TokenizeToTokens(new Token(text)))
        {
            IReadOnlyList<uint> positions = TokenAlignment.SourcePositions(token);
            result.Tokens.Add(token.Text);
            result.Offsets.Add(positions.Count == 0 ? null : new Offset(positions.Min(), checked(positions.Max() + 1)));
            result.ReferenceOffsets.Add(positions.ToArray());
            result.Masks.Add(token.Mask);
        }
        return result;
    }

    public virtual List<Token> TokenizeToTokens(Token initialToken)
    {
        ArgumentNullException.ThrowIfNull(initialToken);
        var result = new List<Token>();
        // Recognize special tokens before cleaning or splitting their content.
        foreach (Token part in SplitOnSpecialTokens(initialToken, vocab))
        {
            if (part.Mask is Mask.Special or Mask.Unknown) { result.Add(part); continue; }
            TokenizationUtils.CleanText(part, true);
            if (lowerCase) TokenizationUtils.Lowercase(part);
            if (stripAccents) TokenizationUtils.StripAccents(part);
            foreach (Token word in Split(part, Rune.IsWhiteSpace, null))
                foreach (Token punctuation in Split(word, Rune.IsPunctuation, Mask.Punctuation))
                    result.AddRange(Split(punctuation, IsCjk, Mask.CJK));
        }
        return result;
    }

    public void DecomposeNfkc(Token token) => TokenizationUtils.DecomposeNfkc(token);
    public List<long> ConvertTokensToIds(List<string> tokens) => tokens.Select(vocab.TokenToId).ToList();

    public TokenizedInput Encode(string inputText, string? additionalInputText, int maxLen,
        TruncationStrategy truncationStrategy, int stride)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLen);
        ArgumentOutOfRangeException.ThrowIfNegative(stride);
        if (!Enum.IsDefined(truncationStrategy)) throw new ArgumentOutOfRangeException(nameof(truncationStrategy));
        TokenIdsWithOffsets Convert(string text)
        {
            var tokens = TokenizeWithOffsets(text);
            return new()
            {
                Ids = ConvertTokensToIds(tokens.Tokens),
                Offsets = tokens.Offsets,
                ReferenceOffsets = tokens.ReferenceOffsets.Select(r => r.ToList()).ToList(),
                Masks = tokens.Masks
            };
        }
        var first = Convert(inputText);
        var second = additionalInputText == null ? null : Convert(additionalInputText);
        int specialCount = BuildInputWithSpecialTokens(new(), second == null ? null : new()).TokenIds.Count;
        if (maxLen < specialCount) throw new ValueTokenizerException("Maximum length is smaller than the required special-token template");
        int remove = Math.Max(0, checked(first.Ids.Count + (second?.Ids.Count ?? 0) + specialCount) - maxLen);
        var (truncatedFirst, truncatedSecond, overflow, _) =
            TokenizationUtils.TruncateSequences(first, second, remove, truncationStrategy, stride);
        var merged = BuildInputWithSpecialTokens(truncatedFirst, truncatedSecond);
        return new()
        {
            TokenIds = merged.TokenIds, SegmentIds = merged.SegmentIds,
            SpecialTokensMask = merged.SpecialTokensMask, OverflowingTokens = overflow,
            NumTruncatedTokens = remove, TokenOffsets = merged.TokenOffsets,
            ReferenceOffsets = merged.ReferenceOffsets, Mask = merged.Mask
        };
    }

    /// <summary>Combine sequences without modifying either input.</summary>
    public virtual TokenIdsWithSpecialTokens BuildInputWithSpecialTokens(TokenIdsWithOffsets inputTokens,
        TokenIdsWithOffsets? additionalInputTokens = null)
    {
        var result = new TokenIdsWithSpecialTokens();
        void Add(TokenIdsWithOffsets input, byte segment)
        {
            result.TokenIds.AddRange(input.Ids);
            result.SegmentIds.AddRange(Enumerable.Repeat(segment, input.Ids.Count));
            result.SpecialTokensMask.AddRange(Enumerable.Repeat((byte)0, input.Ids.Count));
            result.TokenOffsets.AddRange(input.Offsets);
            result.ReferenceOffsets.AddRange(input.ReferenceOffsets);
            result.Mask.AddRange(input.Masks);
        }
        Add(inputTokens, 0);
        if (additionalInputTokens != null) Add(additionalInputTokens, 1);
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

    protected List<Token> SplitOnSpecialTokens(Token token, IVocab vocabulary)
    {
        if (token.Mask is Mask.Special or Mask.Unknown) return new() { token.Clone() };
        var matcher = ReferenceEquals(vocabulary, vocab) ? specialTokens :
            new LiteralTokenMatcher(vocabulary.SpecialValues.Select(p => (p.Key, p.Value)));
        var references = TokenAlignment.References(token);
        var result = new List<Token>();
        int position = 0, scalar = 0, start = 0, startScalar = 0;
        void Add(int end, int endScalar, Mask mask)
        {
            if (end == start) return;
            var part = new Token(token.Text[start..end]) { Mask = mask };
            TokenAlignment.Set(part, part.Text, references.AsSpan(startScalar, endScalar - startScalar).ToArray());
            result.Add(part);
        }
        while (position < token.Text.Length)
        {
            if (!matcher.TryMatch(token.Text.AsSpan(position), out var match))
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
        var runes = token.Text.EnumerateRunes().ToArray();
        var references = TokenAlignment.References(token);
        var result = new List<Token>();
        int start = 0, startUtf16 = 0, utf16 = 0;
        void Add(int begin, int end, int beginUtf16, int endUtf16, Mask mask)
        {
            if (begin == end) return;
            var part = new Token(token.Text[beginUtf16..endUtf16]) { Mask = mask };
            TokenAlignment.Set(part, part.Text, references.AsSpan(begin, end - begin).ToArray());
            result.Add(part);
        }
        for (int i = 0; i < runes.Length; i++)
        {
            int next = utf16 + runes[i].Utf16SequenceLength;
            if (isSeparator(runes[i]))
            {
                Add(start, i, startUtf16, utf16, token.Mask);
                if (separatorMask is { } mask) Add(i, i + 1, utf16, next, mask);
                start = i + 1;
                startUtf16 = next;
            }
            utf16 = next;
        }
        Add(start, runes.Length, startUtf16, utf16, token.Mask);
        return result;
    }

    private static bool IsCjk(Rune rune) => rune.Value is
        >= 0x3400 and <= 0x4DBF or >= 0x4E00 and <= 0x9FFF or >= 0xF900 and <= 0xFAFF or
        >= 0x20000 and <= 0x2EBEF or >= 0x30000 and <= 0x323AF or
        >= 0x3040 and <= 0x30FF or >= 0x31F0 and <= 0x31FF or
        >= 0x1100 and <= 0x11FF or >= 0x3130 and <= 0x318F or
        >= 0xA960 and <= 0xA97F or >= 0xAC00 and <= 0xD7FF;
}

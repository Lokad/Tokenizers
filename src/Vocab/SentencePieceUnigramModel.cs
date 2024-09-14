using Lokad.Tokenizers.Tokenizer;
using System.Text;

namespace Lokad.Tokenizers.Vocab;

/// <summary>
/// Represents a SentencePiece model for tokenization.
/// </summary>
public class SentencePieceModel
{
    /// <summary>
    /// Gets or sets the root node of the trie.
    /// </summary>
    public TrieNode Root { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SentencePieceModel"/> class.
    /// </summary>
    public SentencePieceModel()
    {
        Root = new TrieNode("");
    }

    /// <summary>
    /// Creates a <see cref="SentencePieceModel"/> from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A <see cref="SentencePieceModel"/> instance.</returns>
    public static SentencePieceModel FromFile(string path)
    {
        var model = new SentencePieceModel();
        var proto = VocabHelper.OpenProtobufFile(path);

        foreach (var piece in proto.Pieces.Select((p, i) => new { p.Piece, p.Score, Index = i }))
        {
            model.Insert(piece.Piece, piece.Score, piece.Index);
        }

        return model;
    }

    /// <summary>
    /// Inserts a word into the trie.
    /// </summary>
    /// <param name="word">The word to insert.</param>
    /// <param name="score">The score of the word.</param>
    /// <param name="index">The index of the word.</param>
    private void Insert(string word, float score, int index)
    {
        var node = Root;

        var runes = word.EnumerateRunes().ToList();
        for (var i = 0; i < runes.Count; i++)
        {
            var character = runes[i];
            if (!node.Children.ContainsKey(character))
            {
                var text = node.Text + character;
                node.Children[character] = new TrieNode(text);
            }

            node = node.Children[character];

            if (i == runes.Count - 1)
            {
                node.End = true;
                node.Score = score;
                node.Index = index;
            }
        }
    }

    /// <summary>
    /// Searches for common prefixes in the trie.
    /// </summary>
    /// <param name="text">The text to search for.</param>
    /// <returns>A list of <see cref="TrieNode"/> representing the common prefixes.</returns>
    public List<TrieNode> CommonPrefixSearch(string text)
    {
        var results = new List<TrieNode>();
        var characters = text.EnumerateRunes().ToList();
        if (characters.Count == 0) return results;
        var currentChar = characters[0];
        var node = Root.Children.GetValueOrDefault(currentChar);

        if (node != null)
        {
            if (node.End)
            {
                results.Add(node);
            }
        }
        else
        {
            return results;
        }

        var remainingCharacters = characters.Skip(1).ToList();
        foreach (var character in remainingCharacters)
        {
            node = node.Children.GetValueOrDefault(character);

            if (node != null)
            {
                if (node.End)
                {
                    results.Add(node);
                }
            }
            else
            {
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Decodes a token forward to find references.
    /// </summary>
    /// <param name="token">The token to decode.</param>
    /// <returns>A list of <see cref="Node"/> representing the decoded references.</returns>
    public List<Node?> DecodeForwardTokenRef(Token token)
    {
        var charPositions = new List<int>();

        var runes = TokenizationUtils.CharIndicesForRunes(token.Text).ToList();
        runes.ForEach((i => charPositions.Add(i.Index)));
        charPositions.Add(TokenizationUtils.GetUtf8BytesCount(token.Text));

        //HINT: When using token.Bytes it is not actually presents the correct byte count
        //      of the text because it the text has been modified by reference.
        //      So, we need to get the byte count of the current text not the original text.
        //      Hence, we should introduce immutable Token structure to avoid this issue.
        var tokenTextBytes = Encoding.UTF8.GetBytes(token.Text);

        var results = new Node?[charPositions.Count];
        var scores = Enumerable.Repeat(float.NegativeInfinity, charPositions.Count).ToArray();
        scores[0] = 0f;

        for (var charStart = 0; charStart < charPositions.Count - 1; charStart++)
        {
            var prefixBytes = TokenizationUtils.SubstringByByteOffset(tokenTextBytes, charPositions[charStart]);
            var prefix = Encoding.UTF8.GetString(prefixBytes);
            var matches = CommonPrefixSearch(prefix.ToString());

            foreach (var node in matches)
            {
                var localScore = scores[charStart] + node.Score;
                var charEnd = charStart + node.Length;

                if (localScore > scores[charEnd])
                {
                    var matchedBytes = TokenizationUtils.SubstringByByteOffset(tokenTextBytes, charPositions[charStart], charPositions[charEnd]);
                    var matchedText = Encoding.UTF8.GetString(matchedBytes);
                    results[charEnd] = new Node
                    (
                        text: matchedText,
                        score: localScore,
                        index: node.Index,
                        start: charStart,
                        end: charEnd,
                        referenceOffsets: token.ReferenceOffsets.Skip(charStart).Take(charEnd - charStart).ToArray()
                    );

                    scores[charEnd] = localScore;
                }
            }

            if (scores[charStart + 1] <= float.MinValue)
            {
                var matchedBytes = TokenizationUtils.SubstringByByteOffset(tokenTextBytes, charPositions[charStart], charPositions[charStart + 1]);
                var matchedText = Encoding.UTF8.GetString(matchedBytes);
                results[charStart + 1] = new Node
                (
                    text: matchedText,
                    score: float.MinValue,
                    index: 0,
                    start: charStart,
                    end: charStart + 1,
                    referenceOffsets: token.ReferenceOffsets.Skip(charStart).Take(1).ToArray()
                );

                scores[charStart + 1] = 0f;
            }
        }

        return results.ToList();
    }

    /// <summary>
    /// Decodes nodes backward to find the best sequence.
    /// </summary>
    /// <param name="nodes">The nodes to decode.</param>
    /// <returns>A list of <see cref="Node"/> representing the best sequence.</returns>
    public List<Node> DecodeBackward(Node?[] nodes)
    {
        var bestSequence = new List<Node>();
        var nextNode = nodes.Last();

        while (nextNode != null)
        {
            bestSequence.Add(nextNode);
            nextNode = nodes[nextNode.Start];
        }

        bestSequence.Reverse();

        return bestSequence;
    }

    /// <summary>
    /// Parses nodes to tokens.
    /// </summary>
    /// <param name="nodes">The nodes to parse.</param>
    /// <returns>A list of <see cref="Token"/> representing the parsed tokens.</returns>
    public List<Token> ParseNodesToTokens(List<Node> nodes)
    {
        var output = new List<Token>(nodes.Count + 1);
        var isPrevUnknown = false;

        foreach (var node in nodes)
        {
            // Group unknown tokens
            if (isPrevUnknown && (node.Index == 0))
            {
                var prevToken = output.Last();
                var text = new StringBuilder(prevToken.Text);
                text.Append(node.Text);
                var referenceOffsets = new List<uint>();
                referenceOffsets.AddRange(node.ReferenceOffsets);
                var consolidatedUnknown = new Token(Encoding.UTF8.GetBytes(text.ToString()),
                    offset: new Offset(0, 0),
                    referenceOffsets: referenceOffsets,
                    mask: Mask.Unknown
                );
                output.RemoveAt(output.Count - 1);
                output.Add(consolidatedUnknown);
            }
            else
            {
                output.Add(new Token(Encoding.UTF8.GetBytes(node.Text),
                    offset: new Offset(0, 0),
                    referenceOffsets: node.ReferenceOffsets.ToList(),
                    mask: Mask.None
                ));
            }
            isPrevUnknown = node.Index == 0;
        }

        PopulateMasks(output, Constants.LowerOneEighthBlock);
        return output;
    }

    /// <summary>
    /// Populates masks for tokens.
    /// </summary>
    /// <param name="tokens">The tokens to populate masks for.</param>
    /// <param name="whitespaceToken">The whitespace token.</param>
    public void PopulateMasks(List<Token> tokens, char whitespaceToken)
    {
        var previousMask = Mask.None;

        foreach (var token in tokens)
        {
            if (token.Text.Length == 1)
            {
                var firstChar = token.Text.Last();

                if (char.IsPunctuation(firstChar))
                {
                    token.Mask = Mask.Punctuation;
                    previousMask = Mask.Punctuation;
                    continue;
                }

                if (char.IsWhiteSpace(firstChar))
                {
                    token.Mask = Mask.Whitespace;
                    previousMask = Mask.Punctuation;
                    continue;
                }
            }

            if (!token.Text.StartsWith(whitespaceToken) && previousMask != Mask.Punctuation && previousMask != Mask.Whitespace)
            {
                token.Mask = Mask.Continuation;
                previousMask = Mask.Continuation;
            }
            else
            {
                previousMask = Mask.None;
            }
        }
    }
}

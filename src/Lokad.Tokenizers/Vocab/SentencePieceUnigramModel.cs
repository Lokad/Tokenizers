using Lokad.Tokenizers.Tokenizer;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Lokad.Tokenizers.Vocab;

// TODO: Port by ChatGPT https://github.com/guillaume-be/rust-tokenizers/blob/main/main/src/vocab/sentence_piece_unigram_model.rs

public class Node
{
    public string Text { get; set; }
    public float Score { get; set; }
    public long Index { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public uint[] ReferenceOffsets { get; set; }
}

public class TrieNode
{
    public string Text { get; set; }
    public int Len { get; set; }
    public float Score { get; set; }
    public long Index { get; set; }
    public bool End { get; set; }
    public Dictionary<Rune, TrieNode> Children { get; set; }

    public TrieNode(string text)
    {
        Text = text;
        Len = new StringInfo(text).LengthInTextElements;
        Children = new Dictionary<Rune, TrieNode>();
    }
}

public class SentencePieceModel
{
    public TrieNode Root { get; set; }

    public SentencePieceModel()
    {
        Root = new TrieNode("");
    }

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

    private void Insert(string word, float score, int index)
    {
        var node = Root;

        var runes = word.EnumerateRunes().ToList();
        for (int i = 0; i < runes.Count; i++)
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

    public List<Node?> DecodeForwardTokenRef(Token token)
    {
        List<int> charPositions = new List<int>();

        var runes = TokenizationUtils.CharIndicesForRunes(token.Text).ToList();
        runes.ForEach((i => charPositions.Add(i.Index)));
        charPositions.Add(TokenizationUtils.GetUtf8BytesCount(token.Text));

        var results = new Node?[charPositions.Count];
        var scores = Enumerable.Repeat(float.NegativeInfinity, charPositions.Count).ToArray();
        scores[0] = 0f;

        for (var charStart = 0; charStart < charPositions.Count - 1; charStart++)
        {
            var prefix = TokenizationUtils.SubstringByByteOffset(token.Text, charPositions[charStart]);
            //token.Text.EnumerateRunes().Skip(charPositions[charStart]).ToList().ForEach(r => prefix.Append(r.ToString()));
            var matches = CommonPrefixSearch(prefix.ToString());

            foreach (var node in matches)
            {
                var localScore = scores[charStart] + node.Score;
                var charEnd = charStart + node.Len;

                if (localScore > scores[charEnd])
                {
                    var t = TokenizationUtils.SubstringByByteOffset(token.Text, charPositions[charStart], charPositions[charEnd]);
                    results[charEnd] = new Node
                    {
                        Text = t,
                        Score = localScore,
                        Index = node.Index,
                        Start = charStart,
                        End = charEnd,
                        ReferenceOffsets = token.ReferenceOffsets.Skip(charStart).Take(charEnd - charStart).ToArray()
                    };

                    scores[charEnd] = localScore;
                }
            }

            if (scores[charStart + 1] <= float.MinValue)
            {
                var t = TokenizationUtils.SubstringByByteOffset(token.Text, charPositions[charStart], charPositions[charStart + 1]);
                results[charStart + 1] = new Node
                {
                    Text = t,
                    Score = float.MinValue,
                    Index = 0,
                    Start = charStart,
                    End = charStart + 1,
                    ReferenceOffsets = token.ReferenceOffsets.Skip(charStart).Take(1).ToArray()
                };

                scores[charStart + 1] = 0f;
            }
        }

        return results.ToList();
    }
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

    public List<Token> ParseNodesToTokens(List<Node> nodes)
    {
        List<Token> output = new List<Token>(nodes.Count + 1);
        bool isPrevUnknown = false;

        foreach (Node node in nodes)
        {
            // Group unknown tokens
            if (isPrevUnknown && (node.Index == 0))
            {
                Token prevToken = output.Last();
                StringBuilder text = new StringBuilder(prevToken.Text);
                text.Append(node.Text);
                List<uint> referenceOffsets = new List<uint>();
                referenceOffsets.AddRange(node.ReferenceOffsets);
                Token consolidatedUnknown = new Token(text.ToString())
                {
                    Text = text.ToString(),
                    Offset = new Offset(0, 0),
                    ReferenceOffsets = referenceOffsets,
                    Mask = Mask.Unknown,
                };
                output.RemoveAt(output.Count - 1);
                output.Add(consolidatedUnknown);
            }
            else
            {
                output.Add(new Token(node.Text)
                {
                    Text = node.Text,
                    Offset = new Offset(0, 0),
                    ReferenceOffsets = node.ReferenceOffsets.ToList(),
                    Mask = Mask.None,
                });
            }
            isPrevUnknown = node.Index == 0;
        }

        PopulateMasks(output, '\u2581');
        return output;
    }

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
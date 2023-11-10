using Lokad.Tokenizers.Tokenizer;

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
    public Dictionary<char, TrieNode> Children { get; set; }

    public TrieNode(string text)
    {
        Text = text;
        Len = text.Length;
        Children = new Dictionary<char, TrieNode>();
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

        foreach (var character in word)
        {
            if (!node.Children.ContainsKey(character))
            {
                var text = node.Text + character;
                node.Children[character] = new TrieNode(text);
            }

            node = node.Children[character];

            if (word.Last() == character)
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
        var characters = text.ToCharArray();
        var node = Root.Children.GetValueOrDefault(characters[0]);

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

        foreach (var character in characters.Skip(1))
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
        var charPositions = token.Text.Select((c, i) => i).ToList();
        charPositions.Add(token.Text.Length);

        var results = new Node?[charPositions.Count];
        var scores = Enumerable.Repeat(float.NegativeInfinity, charPositions.Count).ToArray();
        scores[0] = 0f;

        for (var charStart = 0; charStart < charPositions.Count - 1; charStart++)
        {
            var matches = CommonPrefixSearch(token.Text.Substring(charPositions[charStart]));

            foreach (var node in matches)
            {
                var localScore = scores[charStart] + node.Score;
                var charEnd = charStart + node.Len;

                if (localScore > scores[charEnd])
                {
                    results[charEnd] = new Node
                    {
                        Text = token.Text.Substring(charPositions[charStart], charPositions[charEnd] - charPositions[charStart]),
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
                results[charStart + 1] = new Node
                {
                    Text = token.Text.Substring(charPositions[charStart], charPositions[charStart + 1] - charPositions[charStart]),
                    Score = float.MinValue,
                    Index = 0,
                    Start = charStart,
                    End = charStart + 1,
                    ReferenceOffsets = token.ReferenceOffsets.Skip(charStart).Take(charStart + 1 - charStart).ToArray()
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
        var output = new List<Token>();
        var isPrevUnknown = false;

        foreach (var node in nodes)
        {
            if (isPrevUnknown && node.Index == 0)
            {
                var prevToken = output.Last();
                prevToken.Text += node.Text;
                prevToken.ReferenceOffsets = prevToken.ReferenceOffsets.Concat(node.ReferenceOffsets).ToArray();
            }
            else
            {
                output.Add(new Token(node.Text, node.ReferenceOffsets));
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
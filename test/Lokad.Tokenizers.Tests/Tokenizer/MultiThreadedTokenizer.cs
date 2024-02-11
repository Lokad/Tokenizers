using Lokad.Tokenizers.Tokenizer;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public class MultiThreadedTokenizer
{
    public static List<TokenizedInput> EncodeList(XLMRobertaTokenizer tokenizer, string[] original_strings, int maxLength, string truncationStrategy, int numTruncatedTokens)
    {
        var output = new List<TokenizedInput>();
        for (int idx = 0; idx < original_strings.Length; idx++)
        {
            var original_sentence_chars = original_strings[idx].ToCharArray();
            var predicted = new TokenizedInput
            {
                TokenIds = new List<long>(),
                SegmentIds = new List<byte>(),
                SpecialTokensMask = new List<byte>(),
                OverflowingTokens = new List<long>(),
                NumTruncatedTokens = 0,
                TokenOffsets = new List<Offset?>(),
                ReferenceOffsets = new List<List<uint>>(),
                Mask = new List<Mask>()
            };

            for (int i = 0; i < original_sentence_chars.Length; i++)
            {
                var offset = predicted.TokenOffsets.FirstOrDefault();
                if (offset != null)
                {
                    var start_char = offset.Begin;
                    var end_char = offset.End;
                    var text = new string(original_sentence_chars.Skip((int)start_char).Take((int)end_char - (int)start_char).ToArray());
                    Console.WriteLine($"{offset} | {text} | {tokenizer.Decode(new List<long> { predicted.TokenIds[i] }, false, false)} | {predicted.Mask[i]}");
                }
            }

            output.Add(predicted);
        }

        return output;
    }
}

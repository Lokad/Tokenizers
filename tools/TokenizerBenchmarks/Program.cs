using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;

void Measure(string name, Action action)
{
    action();
    long before = GC.GetAllocatedBytesForCurrentThread();
    var clock = Stopwatch.StartNew();
    for (int i = 0; i < 3; i++) action();
    clock.Stop();
    Console.WriteLine(JsonSerializer.Serialize(new { name, allocatedBytes = (GC.GetAllocatedBytesForCurrentThread() - before) / 3,
        milliseconds = clock.Elapsed.TotalMilliseconds / 3 }));
}
var model = new SentencePieceModel();
model.Root.Children[new Rune('a')] = new TrieNode("a") { End = true, Index = 3, Score = -1 };
foreach (int length in new[] { 1000, 2000, 4000 })
{
    var token = new Token(new string('a', length));
    Measure("unigram-" + length, () => model.DecodeForwardTokenRef(token));
}
if (args.Length > 0)
{
    var tokenizer = new GemmaTokenizer(args[0]);
    foreach (int count in new[] { 2000, 4000, 8000 })
    {
        string text = string.Concat(Enumerable.Repeat("<bos>", count));
        Measure("gemma-specials-" + count, () => tokenizer.Encode(text));
    }
    string longText = string.Concat(Enumerable.Repeat("Hello, world! ", 10000));
    Measure("gemma-limit-two", () => {
        try { tokenizer.EncodeBatch(new[] { longText }, 2); throw new InvalidOperationException("Expected overflow"); }
        catch (ArgumentException) { }
    });
}
if (args.Length > 1)
    Measure("xlm-constructor", () => _ = new XLMRobertaTokenizer(args[1], false));

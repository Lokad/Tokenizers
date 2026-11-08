using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Lokad.Tokenizers.Tokenizer;
using Lokad.Tokenizers.Vocab;

// Inputs and model setup are outside steady-state measurements.
// Construction is a separate single sample; steady workloads use one warm-up and three iterations.
void Measure(string name, Action action)
{
    action();
    long before = GC.GetAllocatedBytesForCurrentThread();
    long start = Stopwatch.GetTimestamp();
    for (int i = 0; i < 3; i++) action();
    double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds / 3;
    long allocated = (GC.GetAllocatedBytesForCurrentThread() - before) / 3;
    Console.WriteLine(JsonSerializer.Serialize(new { name, iterations = 3, allocatedBytes = allocated, milliseconds = elapsed }));
}

T Construct<T>(string name, Func<T> factory) where T : class
{
    long retainedBefore = GC.GetTotalMemory(true);
    long before = GC.GetAllocatedBytesForCurrentThread();
    long start = Stopwatch.GetTimestamp();
    T value = factory();
    double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    long retained = GC.GetTotalMemory(true) - retainedBefore;
    GC.KeepAlive(value);
    Console.WriteLine(JsonSerializer.Serialize(new { name, iterations = 1, allocatedBytes = allocated, retainedBytes = retained, milliseconds = elapsed }));
    return value;
}

string Repeat(string text, int length) => string.Concat(Enumerable.Repeat(text, length / text.Length + 1))[..length];

var model = new SentencePieceModel();
model.Root.Children[new Rune('a')] = new TrieNode("a") { End = true, Index = 3, Score = -1 };
foreach (int length in new[] { 1000, 2000, 4000 })
{
    var token = new Token(new string('a', length));
    Measure("unigram-" + length, () => model.DecodeForwardTokenRef(token));
    string prefix = new('a', length);
    var literalVocabulary = new BaseVocab(new() { ["[UNK]"]=0, [prefix+"b"]=1 },
        new() { AdditionalSpecialTokens = new() { prefix+"b" } });
    var literals = new BaseTokenizer<BaseVocab>(literalVocabulary, false, false);
    Measure("literal-long-prefix-miss-" + length, () => literals.Tokenize(prefix));
    string exact = prefix + "b";
    Measure("literal-long-prefix-exact-" + length, () => literals.Tokenize(exact));
}

foreach (int size in new[] { 10000, 100000 })
{
    BaseVocab Vocabulary()
    {
        var values = Enumerable.Range(0, size).ToDictionary(i => "v" + i, i => (long)i);
        values["[UNK]"] = size;
        return new(values, new());
    }
    var incremental = Vocabulary(); var batched = Vocabulary(); var unchanged = Vocabulary();
    string[][] inputs = Enumerable.Range(0, 4).Select(round =>
        Enumerable.Range(0, 100).Select(i => $"new-{round}-{i}").ToArray()).ToArray();
    int iteration = 0;
    Measure("vocab-incremental-" + size, () => {
        foreach (string token in inputs[iteration++]) incremental.AddTokens(new[] { token });
    });
    iteration = 0;
    Measure("vocab-batched-" + size, () => batched.AddTokens(inputs[iteration++]));
    string[] existing = ["v0"];
    Measure("vocab-noop-" + size, () => {
        for (int i = 0; i < 100; i++) unchanged.AddTokens(existing);
    });
}

if (args.Length > 0)
{
    var tokenizer = Construct("gemma-constructor", () => new GemmaTokenizer(args[0]));
    foreach (int length in new[] { 1000, 4000 })
    {
        string ascii = Repeat("Hello, world! ", length), unicode = Repeat("é e\u0301 🧪東京 ", length);
        Measure("gemma-ascii-" + length, () => tokenizer.Encode(ascii));
        Measure("gemma-unicode-" + length, () => tokenizer.Encode(unicode));
    }
    foreach (int count in new[] { 2000, 4000, 8000 })
    {
        string text = string.Concat(Enumerable.Repeat("<bos>", count));
        Measure("gemma-specials-" + count, () => tokenizer.Encode(text));
    }
    string[] rows = Enumerable.Range(0, 8).Select(i => Repeat("Hello, world! 🧪 ", 50 + 20 * i)).ToArray();
    Measure("gemma-batch-eight", () => tokenizer.EncodeBatch(rows, 512));
    string longText = Repeat("Hello, world! ", 140000);
    Measure("gemma-limit-two", () => {
        try { tokenizer.EncodeBatch(new[] { longText }, 2); throw new InvalidOperationException("Expected overflow"); }
        catch (ArgumentException) { }
    });
}

if (args.Length > 1)
{
    var tokenizer = Construct("xlm-constructor", () => new XLMRobertaTokenizer(args[1], false));
    var lowercase = tokenizer.WithLowerCase(true);
    foreach (int length in new[] { 996, 1992, 3996 })
    {
        string text = Repeat("hello world ", length);
        Measure("xlm-tokenize-" + length, () => tokenizer.Tokenize(text));
        Measure("xlm-offsets-" + length, () => tokenizer.TokenizeWithOffsets(text));
        Measure("xlm-ids-" + length, () => tokenizer.ConvertTokensToIds(tokenizer.Tokenize(text)));
        Measure("xlm-encode-truncate-" + length, () => tokenizer.Encode(text, null, 128, TruncationStrategy.LongestFirst, 0));
    }
    string normalized = Repeat("İ e\u0301 a\u0301\u0327 가 ﬁ ", 2000);
    Measure("xlm-normalize-and-lowercase", () => lowercase.TokenizeWithOffsets(normalized));
    string a = Repeat("hello world ", 1000), b = Repeat("paired input ", 800);
    Measure("xlm-pair-truncate", () => tokenizer.Encode(a, b, 128, TruncationStrategy.LongestFirst, 16));

    var template = VocabHelper.OpenProtobufFile(args[1]);
    foreach (int length in new[] { 1000, 2000, 4000 })
    {
        var tiny = new ModelProto { TrainerSpec = template.TrainerSpec.Clone(), NormalizerSpec = template.NormalizerSpec.Clone() };
        tiny.Pieces.Add(template.Pieces.Take(3).Select(p => p.Clone()));
        tiny.Pieces.Add(new ModelProto.Types.SentencePiece { Piece = new string('a', length), Score = -1 });
        tiny.TrainerSpec.VocabSize = 4;
        string path = Path.GetTempFileName();
        try
        {
            using (var stream = File.Create(path)) Google.Protobuf.MessageExtensions.WriteTo(tiny, stream);
            Measure("xlm-long-piece-constructor-" + length, () => SentencePieceModel.FromFile(path));
            var longModel = SentencePieceModel.FromFile(path);
            var exact = new Token(new string('a', length));
            var miss = new Token(new string('a', length - 1) + "b");
            Measure("xlm-long-prefix-exact-" + length, () => longModel.DecodeForwardTokenRef(exact));
            Measure("xlm-long-prefix-miss-" + length, () => longModel.DecodeForwardTokenRef(miss));
        }
        finally { File.Delete(path); }
    }
}

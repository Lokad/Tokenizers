using System.Security.Cryptography;
using System.Text.Json;
using Lokad.Tokenizers.Tokenizer;

if (args.Length != 3) throw new ArgumentException("Usage: GemmaConformance tokenizer.json reference.json actual.json");
var tokenizer = new GemmaTokenizer(args[0]);
using var reference = JsonDocument.Parse(File.ReadAllBytes(args[1]));
JsonElement root = reference.RootElement;
string[] texts = root.GetProperty("cases").EnumerateArray().Select(c => c.GetProperty("text").GetString()!).ToArray();
var cases = new List<object>();
int index = 0;
foreach (var item in root.GetProperty("cases").EnumerateArray())
{
    string text = texts[index++];
    long[] ids = tokenizer.Encode(text);
    var exact = tokenizer.EncodeBatch(new[] { text }, ids.Length);
    if (!ids.SequenceEqual(exact.InputIds)) throw new InvalidOperationException("Exact limit changed IDs");
    if (ids.Length > 2)
    {
        bool rejected = false;
        try { tokenizer.EncodeBatch(new[] { text }, ids.Length - 1); }
        catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("Overflow was silently truncated");
    }
    cases.Add(new { id = item.GetProperty("id").GetString(), ids, rawIds = tokenizer.Encode(text, false) });
}
var batches = new List<object>();
foreach (var item in root.GetProperty("batches").EnumerateArray())
{
    string[] selected = item.GetProperty("indices").EnumerateArray().Select(i => texts[i.GetInt32()]).ToArray();
    var result = tokenizer.EncodeBatch(selected, int.MaxValue);
    batches.Add(new { id = item.GetProperty("id").GetString(), batchSize = result.BatchSize, sequenceLength = result.SequenceLength,
        tokenCounts = result.TokenCounts, inputIds = result.InputIds, attentionMask = result.AttentionMask });
}
string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
File.WriteAllText(args[2], JsonSerializer.Serialize(new { schemaVersion = 1, tokenizerSha256 = tokenizer.TokenizerSha256,
    referenceSha256 = Hash(args[1]), librarySha256 = Hash(typeof(GemmaTokenizer).Assembly.Location),
    runnerSha256 = Hash(typeof(Program).Assembly.Location), cases, batches }));
Console.WriteLine($"PASS managed generation and token limits: {cases.Count} cases, {batches.Count} batches");

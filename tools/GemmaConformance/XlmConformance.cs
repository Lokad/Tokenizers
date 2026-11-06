using System.Security.Cryptography;
using System.Text.Json;
using Lokad.Tokenizers.Tokenizer;

internal static class XlmConformance
{
    internal static void Run(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Expected XLM-R model, reference JSON, output JSON");
        var normal = new XLMRobertaTokenizer(args[0],false);
        var lowercase = new XLMRobertaTokenizer(args[0],true);
        using var document = JsonDocument.Parse(File.ReadAllBytes(args[1]));
        var cases = new List<object>();
        foreach (var item in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var tokenizer = item.GetProperty("lowercase").GetBoolean() ? lowercase : normal;
            var result = tokenizer.Encode(item.GetProperty("text").GetString()!,null,item.GetProperty("maximum").GetInt32(),TruncationStrategy.OnlyFirst,0);
            cases.Add(new { id = item.GetProperty("id").GetString(),ids = result.TokenIds,
                offsets = result.TokenOffsets.Select(o => o == null ? null : new[] { o.Begin,o.End }) });
        }
        string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
        File.WriteAllText(args[2],JsonSerializer.Serialize(new { schemaVersion = 1,tokenizerSha256 = Hash(args[0]),
            referenceSha256 = Hash(args[1]),librarySha256 = Hash(typeof(XLMRobertaTokenizer).Assembly.Location),cases }));
        Console.WriteLine($"PASS managed XLM-R generation: {cases.Count} cases");
    }
}

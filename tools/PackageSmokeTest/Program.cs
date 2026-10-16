using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text.Json;
using Lokad.Tokenizers.Tokenizer;

if (args.Length != 5) throw new ArgumentException("Expected version, tokenizer, symbol package, commit, and XLM-R fixture");
string version = args[0], commit = args[3];
if (typeof(GemmaTokenizer).Assembly.GetName().Version?.ToString(3) != version)
    throw new InvalidOperationException("Unexpected package assembly version");
var tokenizer = new GemmaTokenizer(args[1]);
if (tokenizer.TokenizerSha256 != "4d777ef5bdc1aa36227abdfb77c3e49e7b9c892d16e1b6bda41c393504828be4")
    throw new InvalidDataException("Changed tokenizer asset");
long[] expected = { 2, 9259, 236764, 1902, 236888, 1 };
if (!expected.SequenceEqual(tokenizer.Encode("Hello, world!"))) throw new InvalidOperationException("Package Gemma encoding differs");
var batch = tokenizer.EncodeBatch(new[] { "", "Hello, world!" }, 6);
if (batch.BatchSize != 2 || batch.SequenceLength != 6 || !batch.TokenCounts.SequenceEqual(new[] { 2, 6 }) ||
    !batch.InputIds.SequenceEqual(new long[] { 2, 1, 0, 0, 0, 0 }.Concat(expected)) ||
    !batch.AttentionMask.SequenceEqual(new long[] { 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1 }))
    throw new InvalidOperationException("Package right padding differs");
var legacy = new XLMRobertaTokenizer(args[4], false);
var legacyIds = legacy.Encode("…", null, 128, TruncationStrategy.LongestFirst, 0).TokenIds;
if (!legacyIds.SequenceEqual(new long[] { 0, 153, 2 }))
    throw new InvalidOperationException("Package XLM-R encoding differs");

using var symbols = ZipFile.OpenRead(args[2]);
using var pdbStream = new MemoryStream();
using (var source = symbols.GetEntry("lib/net10.0/Lokad.Tokenizers.pdb")!.Open()) source.CopyTo(pdbStream);
pdbStream.Position = 0;
using var metadata = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
MetadataReader reader = metadata.GetMetadataReader();
bool sourceLink = false;
foreach (var handle in reader.CustomDebugInformation)
{
    byte[] blob = reader.GetBlobBytes(reader.GetCustomDebugInformation(handle).Value);
    if (blob.Length == 0 || blob[0] != (byte)'{') continue;
    try
    {
        using var json = JsonDocument.Parse(blob);
        if (json.RootElement.TryGetProperty("documents", out var documents))
            sourceLink |= documents.EnumerateObject().Any(p => p.Value.GetString()?.StartsWith(
                "https://raw.githubusercontent.com/Lokad/Tokenizers/" + commit + "/", StringComparison.OrdinalIgnoreCase) == true);
    }
    catch (JsonException) { }
}
if (!sourceLink) throw new InvalidDataException("Portable PDB has no matching GitHub SourceLink mapping");
Console.WriteLine("PASS isolated package consumer, XLM-R/Gemma APIs, and SourceLink: " + version);

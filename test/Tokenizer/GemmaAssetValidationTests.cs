using System.Text.Json.Nodes;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class GemmaAssetValidationTests
{
    [Theory]
    [InlineData("duplicate-id")]
    [InlineData("negative-id")]
    [InlineData("missing-byte")]
    [InlineData("unknown-merge")]
    [InlineData("missing-merge-result")]
    [InlineData("duplicate-merge")]
    [InlineData("added-id")]
    [InlineData("duplicate-added")]
    public void InvalidAssetsHaveUsefulDeterministicDiagnostics(string setting)
    {
        var json = JsonNode.Parse(GemmaTokenizerTests.FixtureJson())!;
        switch (setting)
        {
            case "duplicate-id": json["model"]!["vocab"]!["b"] = 10; break;
            case "negative-id": json["model"]!["vocab"]!["b"] = -1; break;
            case "missing-byte": json["model"]!["vocab"]!.AsObject().Remove("<0xFF>"); break;
            case "unknown-merge": json["model"]!["merges"]![0]![0] = "missing"; break;
            case "missing-merge-result": json["model"]!["vocab"]!.AsObject().Remove("bc"); break;
            case "duplicate-merge": json["model"]!["merges"]!.AsArray().Add(json["model"]!["merges"]![0]!.DeepClone()); break;
            case "added-id": json["added_tokens"]![0]!["id"] = 999; break;
            case "duplicate-added": json["added_tokens"]!.AsArray().Add(json["added_tokens"]![0]!.DeepClone()); break;
        }
        WithAsset(json,path => {
            var first = Assert.Throws<InvalidDataException>(() => new GemmaTokenizer(path));
            var second = Assert.Throws<InvalidDataException>(() => new GemmaTokenizer(path));
            Assert.Equal(first.Message,second.Message);
            Assert.False(string.IsNullOrWhiteSpace(first.Message));
        });
    }

    [Theory]
    [InlineData("single_word")]
    [InlineData("lstrip")]
    [InlineData("rstrip")]
    [InlineData("normalized")]
    [InlineData("special")]
    [InlineData("normalizer")]
    [InlineData("template")]
    [InlineData("template-id")]
    [InlineData("padding")]
    public void IncompatiblePipelinesAreRejectedBeforeEncoding(string setting)
    {
        var json = JsonNode.Parse(GemmaTokenizerTests.FixtureJson())!;
        if (setting is "single_word" or "lstrip" or "rstrip" or "normalized") json["added_tokens"]![0]![setting] = true;
        else if (setting == "special") json["added_tokens"]![0]![setting] = false;
        else if (setting == "normalizer") json["normalizer"]!["content"] = "x";
        else if (setting == "template") json["post_processor"]!["single"]![0]!["SpecialToken"]!["id"] = "<eos>";
        else if (setting == "template-id") json["post_processor"]!["special_tokens"]!["<bos>"]!["ids"]![0] = 1;
        else json["padding"] = new JsonObject { ["direction"] = "Left" };
        WithAsset(json,path => Assert.Contains("Unsupported Gemma tokenizer",Assert.Throws<NotSupportedException>(() => new GemmaTokenizer(path)).Message));
    }

    static void WithAsset(JsonNode json,Action<string> action)
    {
        string path = Path.GetTempFileName();
        try { File.WriteAllText(path,json.ToJsonString()); action(path); }
        finally { File.Delete(path); }
    }
}

using System.Text.Json.Nodes;
using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class GemmaAssetValidationTests
{
    [Theory]
    [InlineData("\"version\":\"1.0\"", "\"version\":\"\\uD800\"", "tokenizer.version")]
    [InlineData("\"version\":\"1.0\"", "\"\\uD800\":\"1.0\"", "tokenizer")]
    [InlineData("\"vocab\":{", "\"vocab\":{\"\\uD800\":999,", "model.vocab")]
    public void MalformedUnicodeStringsIdentifyTheirSetting(string original, string replacement, string path)
    {
        string file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, GemmaTokenizerTests.FixtureJson().Replace(original, replacement, StringComparison.Ordinal));
            Assert.Contains(path, Assert.Throws<InvalidDataException>(() => new GemmaTokenizer(file)).Message);
        }
        finally { File.Delete(file); }
    }

    [Theory]
    [InlineData("🧪")]
    [InlineData("\uFFFE")]
    public void EscapedValidUnicodeVocabularyNamesRemainSupported(string text)
    {
        var json = JsonNode.Parse(GemmaTokenizerTests.FixtureJson())!;
        json["model"]!["vocab"]![text] = 999;
        WithAsset(json, file => Assert.Equal(new long[] { 999 }, new GemmaTokenizer(file).Encode(text, false)));
    }

    [Theory]
    [InlineData("missing-model","model")]
    [InlineData("null-model","model")]
    [InlineData("missing-normalizer","normalizer")]
    [InlineData("null-content","added_tokens[].content")]
    [InlineData("number-content","added_tokens[].content")]
    [InlineData("null-merge","model.merges[][0]")]
    [InlineData("wrong-vocab-shape","model.vocab")]
    [InlineData("wrong-id-type","model.vocab")]
    public void MalformedShapesIdentifyTheSetting(string mutation,string path)
    {
        var json=JsonNode.Parse(GemmaTokenizerTests.FixtureJson())!;
        switch(mutation)
        {
            case "missing-model":json.AsObject().Remove("model");break;
            case "null-model":json["model"]=null;break;
            case "missing-normalizer":json.AsObject().Remove("normalizer");break;
            case "null-content":json["added_tokens"]![0]!["content"]=null;break;
            case "number-content":json["added_tokens"]![0]!["content"]=42;break;
            case "null-merge":json["model"]!["merges"]![0]![0]=null;break;
            case "wrong-vocab-shape":json["model"]!["vocab"]=new JsonArray();break;
            case "wrong-id-type":json["model"]!["vocab"]!["a"]="not an ID";break;
        }
        WithAsset(json,file=>Assert.Contains(path,Assert.Throws<InvalidDataException>(()=>new GemmaTokenizer(file)).Message));
    }

    [Fact]
    public void DuplicateObjectFieldsHaveSettingPaths()
    {
        string file=Path.GetTempFileName();
        try
        {
            File.WriteAllText(file,GemmaTokenizerTests.FixtureJson().Replace("\"version\":\"1.0\"","\"version\":\"1.0\",\"version\":\"1.0\"",StringComparison.Ordinal));
            Assert.Contains("tokenizer.version",Assert.Throws<InvalidDataException>(()=>new GemmaTokenizer(file)).Message);
        }
        finally{File.Delete(file);}
    }

    [Theory]
    [InlineData("duplicate-id","model.vocab")]
    [InlineData("negative-id","model.vocab")]
    [InlineData("missing-byte","model.vocab")]
    [InlineData("unknown-merge","model.merges")]
    [InlineData("missing-merge-result","model.merges")]
    [InlineData("duplicate-merge","model.merges")]
    [InlineData("added-id","added_tokens")]
    [InlineData("duplicate-added","added_tokens")]
    public void InvalidAssetsIdentifyTheAffectedSetting(string setting,string expectedPath)
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
            Assert.Contains(expectedPath,first.Message);
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

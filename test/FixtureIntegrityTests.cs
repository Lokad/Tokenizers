using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Lokad.Tokenizers.Tests;

public sealed class FixtureIntegrityTests
{
    [Fact]
    public void CheckedInFixturesMatchTheirIntegrityManifest()
    {
        string directory = Path.Combine(AppContext.BaseDirectory,"Fixture");
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory,"fixtures.json")));
        Assert.Equal(1,document.RootElement.GetProperty("schemaVersion").GetInt32());
        var files = document.RootElement.GetProperty("files").EnumerateArray().ToArray();
        Assert.NotEmpty(files);
        Assert.Equal(files.Length,files.Select(f => f.GetProperty("path").GetString()).Distinct().Count());
        foreach (var file in files)
        {
            string name = file.GetProperty("path").GetString()!;
            Assert.Equal(name,Path.GetFileName(name));
            string path = Path.Combine(directory,name);
            Assert.Equal(file.GetProperty("size").GetInt64(),new FileInfo(path).Length);
            using var stream = File.OpenRead(path);
            Assert.Equal(file.GetProperty("sha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(stream)));
        }
    }
}

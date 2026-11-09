using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Lokad.Tokenizers.Qualification;
using Xunit;

namespace Lokad.Tokenizers.Tests.Symbols;

public sealed class SourceLinkTests
{
    private const string Commit="0123456789abcdef0123456789abcdef01234567";
    private static byte[] Pdb(Guid kind,bool module,string pattern)
    {
        var metadata=new MetadataBuilder();
        var document=metadata.AddDocument(metadata.GetOrAddDocumentName("/_/src/File.cs"),default,default,default);
        string json="{\"documents\":{\""+pattern+"\":\"https://raw.githubusercontent.com/Lokad/Tokenizers/"+Commit+"/*\"}}";
        metadata.AddCustomDebugInformation(module ? MetadataTokens.EntityHandle(0x00000001) : document,
            metadata.GetOrAddGuid(kind),metadata.GetOrAddBlob(Encoding.UTF8.GetBytes(json)));
        var counts=new int[MetadataTokens.TableCount]; counts[(int)TableIndex.Module]=1;
        var builder=new PortablePdbBuilder(metadata,ImmutableArray.CreateRange(counts),default);
        var blob=new BlobBuilder(); builder.Serialize(blob);
        return blob.ToArray();
    }

    [Theory]
    [InlineData("kind")]
    [InlineData("parent")]
    [InlineData("map")]
    public void UnusableMetadataCannotSatisfySourceLink(string mutation)
    {
        byte[] data=Pdb(mutation == "kind" ? Guid.Empty : PackageSymbols.SourceLinkKind,mutation != "parent",mutation == "map" ? "/z/*" : "/_/*");
        using var provider=MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(data));
        string message=Assert.Throws<InvalidDataException>(()=>PackageSymbols.ValidateSourceLink(provider.GetMetadataReader(),Commit)).Message;
        Assert.Contains(mutation == "map" ? "does not cover PDB document" : "module SourceLink record",message);
    }

    [Fact]
    public void TypedModuleMappingMustCoverActualDocumentsAtTheExpectedCommit()
    {
        using var provider=MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(Pdb(PackageSymbols.SourceLinkKind,true,"/_/*")));
        var reader=provider.GetMetadataReader();
        PackageSymbols.ValidateSourceLink(reader,Commit);
        Assert.Contains("expected repository commit",Assert.Throws<InvalidDataException>(()=>PackageSymbols.ValidateSourceLink(reader,new string('a',40))).Message);
    }
}

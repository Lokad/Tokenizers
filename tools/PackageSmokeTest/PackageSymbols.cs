using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace Lokad.Tokenizers.Qualification;

internal static class PackageSymbols
{
    internal static readonly Guid SourceLinkKind = new("cc110556-a091-4d38-9fec-25ab9a351a6a");

    internal static void ValidateSourceLink(MetadataReader reader, string commit)
    {
        var records=reader.CustomDebugInformation.Select(reader.GetCustomDebugInformation)
            .Where(record=>record.Parent.Kind == HandleKind.ModuleDefinition && reader.GetGuid(record.Kind) == SourceLinkKind).ToArray();
        if (records.Length != 1) throw new InvalidDataException("Expected exactly one module SourceLink record");
        using var json=JsonDocument.Parse(reader.GetBlobBytes(records[0].Value));
        if (!json.RootElement.TryGetProperty("documents",out var documents) || documents.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("SourceLink documents must be an object");
        var maps=documents.EnumerateObject().Select(p=>(Pattern:p.Name,Target:p.Value.GetString())).ToArray();
        string expected="https://raw.githubusercontent.com/Lokad/Tokenizers/"+commit+"/";
        if (maps.Length == 0 || maps.Select(m=>m.Pattern).Distinct(StringComparer.Ordinal).Count() != maps.Length)
            throw new InvalidDataException("Empty or duplicate SourceLink document mappings");
        foreach (var (pattern,target) in maps)
        {
            if (target == null || !target.StartsWith(expected,StringComparison.Ordinal) || target.Length == expected.Length ||
                pattern.Count(c=>c == '*') > 1 || pattern.Count(c=>c == '*') != target.Count(c=>c == '*') ||
                !Uri.TryCreate(target,UriKind.Absolute,out _))
                throw new InvalidDataException("Invalid SourceLink target for the expected repository commit");
        }
        if (reader.Documents.Count == 0) throw new InvalidDataException("Portable PDB contains no documents");
        foreach (var handle in reader.Documents)
        {
            string name=reader.GetString(reader.GetDocument(handle).Name);
            if (!maps.Any(m=>Matches(m.Pattern,name)))
                throw new InvalidDataException("SourceLink mapping does not cover PDB document: "+name);
        }
    }

    private static bool Matches(string pattern,string name)
    {
        int wildcard=pattern.IndexOf('*');
        return wildcard < 0 ? string.Equals(pattern,name,StringComparison.Ordinal) :
            name.Length >= pattern.Length-1 && name.StartsWith(pattern[..wildcard],StringComparison.Ordinal) &&
            name.EndsWith(pattern[(wildcard+1)..],StringComparison.Ordinal);
    }

    // Package negative controls retain valid metadata and identity except for
    // the one selected field, so failures must come from the intended gate.
    internal static void CreateMutant(string source,string destination,string kind)
    {
        File.Copy(source,destination,true);
        using var archive=ZipFile.Open(destination,ZipArchiveMode.Update);
        var entry=archive.GetEntry("lib/net10.0/Lokad.Tokenizers.pdb") ?? throw new InvalidDataException("Missing PDB");
        byte[] bytes=new byte[checked((int)entry.Length)];
        using (var stream=entry.Open()) stream.ReadExactly(bytes);
        using (var stream=new MemoryStream(bytes,false))
        using (var provider=MetadataReaderProvider.FromPortablePdbStream(stream))
        {
            var reader=provider.GetMetadataReader();
            if (kind == "identity") bytes[reader.DebugMetadataHeader!.IdStartOffset] ^= 1;
            else
            {
                var record=reader.CustomDebugInformation.Select(reader.GetCustomDebugInformation).Single(r=>
                    r.Parent.Kind == HandleKind.ModuleDefinition && reader.GetGuid(r.Kind) == SourceLinkKind);
                if (kind == "source-link-kind")
                {
                    int offset=reader.GetHeapMetadataOffset(HeapIndex.Guid)+(MetadataTokens.GetHeapOffset(record.Kind)-1)*16;
                    bytes[offset] ^= 1;
                }
                else if (kind == "source-link-map")
                {
                    byte[] payload=reader.GetBlobBytes(record.Value);
                    using var json=JsonDocument.Parse(payload);
                    string pattern=json.RootElement.GetProperty("documents").EnumerateObject().First().Name;
                    if (!pattern.StartsWith('/')) throw new InvalidDataException("Expected an absolute mapped document path");
                    int offset=reader.GetHeapMetadataOffset(HeapIndex.Blob)+MetadataTokens.GetHeapOffset(record.Value);
                    int prefix=(bytes[offset]&0x80) == 0 ? 1 : (bytes[offset]&0xC0) == 0x80 ? 2 : 4;
                    int patternOffset=payload.AsSpan().IndexOf(Encoding.UTF8.GetBytes(pattern));
                    if (patternOffset < 0) throw new InvalidDataException("Mapping payload anchor absent");
                    bytes[offset+prefix+patternOffset]=(byte)'!';
                }
                else throw new ArgumentException("Unknown symbol mutation",nameof(kind));
            }
        }
        entry.Delete();
        using var output=archive.CreateEntry("lib/net10.0/Lokad.Tokenizers.pdb").Open();
        output.Write(bytes);
    }
}

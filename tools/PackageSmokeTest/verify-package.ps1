param(
    [string]$OutputDirectory,
    [string]$Tokenizer = 'models/embeddinggemma-2/tokenizer.json'
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $repoRoot
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'artifacts/nuget' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$Tokenizer = [IO.Path]::GetFullPath($Tokenizer)
$project = Join-Path $repoRoot 'src/Lokad.Tokenizers.csproj'
[xml]$projectXml = Get-Content -Raw -LiteralPath $project
$version = [string]$projectXml.Project.PropertyGroup.Version
$commit = (git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine repository commit' }
function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Package check failed ($LASTEXITCODE)" }
}
Invoke-Checked { dotnet pack $project -c Release --tl:off --nologo -v minimal --no-restore -o $OutputDirectory }
$package = Join-Path $OutputDirectory "Lokad.Tokenizers.$version.nupkg"
$symbols = Join-Path $OutputDirectory "Lokad.Tokenizers.$version.snupkg"
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entries = @($archive.Entries.FullName)
    foreach ($required in @('lib/net10.0/Lokad.Tokenizers.dll', 'lib/net10.0/Lokad.Tokenizers.xml', 'README.md', 'CHANGELOG.md', 'LICENSE.txt', 'icon.png')) {
        if ($required -notin $entries) { throw "Package is missing $required" }
    }
    if (@($entries | Where-Object { $_ -like 'lib/*/*.dll' }).Count -ne 1) { throw 'Unexpected package assemblies' }
    $reader = [IO.StreamReader]::new($archive.GetEntry('Lokad.Tokenizers.nuspec').Open())
    try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($nuspec.package.metadata.version -ne $version -or $nuspec.package.metadata.repository.commit -ne $commit) {
        throw 'Package version/repository commit mismatch'
    }
    if (($nuspec.package.metadata.dependencies.group.dependency.id -join ',') -ne 'Google.Protobuf') { throw 'Unexpected runtime dependencies' }
    if ($nuspec.package.metadata.icon -ne 'icon.png' -or $archive.GetEntry('icon.png').Length -gt 1MB) { throw 'Invalid package icon' }
} finally { $archive.Dispose() }

# An external directory and private cache prevent a source project or a stale
# globally cached package from satisfying this consumer.
$consumer = Join-Path ([IO.Path]::GetTempPath()) ('tokenizers-package-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($consumer) | Out-Null
$feed = [Security.SecurityElement]::Escape($OutputDirectory)
@"
<configuration>
  <packageSources><clear /><add key="candidate" value="$feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><packageSource key="candidate"><package pattern="Lokad.Tokenizers" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'NuGet.Config') -Encoding utf8
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors><ImportDirectoryBuildProps>false</ImportDirectoryBuildProps><ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup>
  <ItemGroup><PackageReference Include="Lokad.Tokenizers" Version="[$version]" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'Consumer.csproj') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs') -Destination (Join-Path $consumer 'Program.cs')
$consumerProject = Join-Path $consumer 'Consumer.csproj'
$cache = Join-Path $consumer 'packages'
Invoke-Checked { dotnet restore $consumerProject --configfile (Join-Path $consumer 'NuGet.Config') --packages $cache --tl:off -v minimal }
$restored = Join-Path $cache "lokad.tokenizers/$version/lokad.tokenizers.$version.nupkg"
if ((Get-FileHash -LiteralPath $package -Algorithm SHA512).Hash -ne (Get-FileHash -LiteralPath $restored -Algorithm SHA512).Hash) {
    throw 'Restored package differs from the candidate'
}
$legacy = Join-Path $repoRoot 'test/Fixture/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model'
Invoke-Checked { dotnet build $consumerProject -c Release --no-restore --tl:off --nologo -v minimal }
Invoke-Checked { dotnet (Join-Path $consumer 'bin/Release/net10.0/Consumer.dll') $version $Tokenizer $symbols $commit $legacy }
Write-Host "PASS package contents, symbols, and consumer: $package"
Write-Host "Consumer evidence: $consumer"

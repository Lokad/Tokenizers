param([switch]$Check)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = '3.36.2'
$expectedHash = '9F4A9B0B762421233A9118605FD165E034F19BDCD5991027A9E9356F1341D696'
$directory = Join-Path $repo "artifacts/protobuf-tools/$version"
$package = Join-Path $repo "artifacts/google.protobuf.tools.$version.nupkg"
[IO.Directory]::CreateDirectory((Split-Path -Parent $package)) | Out-Null
if (!(Test-Path -LiteralPath $package)) {
    Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/google.protobuf.tools/$version/google.protobuf.tools.$version.nupkg" -OutFile $package
}
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'The pinned protobuf compiler package checksum differs'
}
[IO.Compression.ZipFile]::ExtractToDirectory($package, $directory, $true)
$architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
if ($IsWindows -and $architecture -eq 'x64') { $relative = 'tools/windows_x64/protoc.exe' }
elseif ($IsLinux -and $architecture -eq 'x64') { $relative = 'tools/linux_x64/protoc' }
elseif ($IsLinux -and $architecture -eq 'arm64') { $relative = 'tools/linux_aarch64/protoc' }
elseif ($IsMacOS -and $architecture -eq 'x64') { $relative = 'tools/macosx_x64/protoc' }
else { throw 'Compiler regeneration supports Windows x64, Linux x64/arm64, and macOS x64' }
$compiler = Join-Path $directory $relative
if (!$IsWindows) { & chmod '+x' $compiler; if ($LASTEXITCODE -ne 0) { throw 'Cannot make protoc executable' } }
$generated = Join-Path $repo 'artifacts/protobuf-generated'
[IO.Directory]::CreateDirectory($generated) | Out-Null
$schema = Join-Path $repo 'src/Vocab/proto'
& $compiler "--proto_path=$schema" "--csharp_out=$generated" (Join-Path $schema 'sentencepiece_model.proto')
if ($LASTEXITCODE -ne 0) { throw 'Protobuf generation failed' }
$source = [IO.File]::ReadAllText((Join-Path $generated 'SentencepieceModel.cs')).Replace("`r`n", "`n")
$target = Join-Path $schema 'SentencepieceModel.cs'
if ($Check) {
    $existing = [IO.File]::ReadAllText($target).Replace("`r`n", "`n")
    if ($source -cne $existing) { throw 'Generated protobuf C# differs. Run eng/generate-protobuf.ps1 and review the change.' }
    Write-Host 'PASS canonical protobuf generation'
} else {
    [IO.File]::WriteAllText($target, $source, [Text.UTF8Encoding]::new($false))
    Write-Host "Generated $target with pinned Google.Protobuf.Tools $version"
}

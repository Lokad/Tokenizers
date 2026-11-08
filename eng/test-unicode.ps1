param([string]$Python = 'python',[switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Unicode qualification failed ($LASTEXITCODE)" }
}
Invoke-Checked { & $Python test/python/unicode_reference.py }
if (!$SkipBuild) { Invoke-Checked { dotnet build tools/GemmaConformance -c Release --tl:off --nologo -v minimal } }
Invoke-Checked { dotnet tools/GemmaConformance/bin/Release/net10.0/GemmaConformance.dll --unicode artifacts/unicode/reference.json artifacts/unicode/actual.json }

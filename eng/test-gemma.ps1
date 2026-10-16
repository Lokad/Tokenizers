param(
    [string]$Python = 'python',
    [string]$Tokenizer = 'models/embeddinggemma-2/tokenizer.json',
    [switch]$Download,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Gemma qualification failed ($LASTEXITCODE)" }
}
$downloadArgs = @()
if ($Download) { $downloadArgs = @('--download') }
Invoke-Checked { & $Python test/python/gemma_reference.py --tokenizer $Tokenizer @downloadArgs }
if (!$SkipBuild) { Invoke-Checked { dotnet build tools/GemmaConformance -c Release --tl:off --nologo -v minimal } }
Invoke-Checked { dotnet tools/GemmaConformance/bin/Release/net10.0/GemmaConformance.dll $Tokenizer artifacts/gemma/reference.json artifacts/gemma/actual.json }
Invoke-Checked { & $Python test/python/gemma_reference.py --tokenizer $Tokenizer --verify artifacts/gemma/actual.json }

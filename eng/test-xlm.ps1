param([string]$Python = 'python',[switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "XLM-R qualification failed ($LASTEXITCODE)" }
}
Invoke-Checked { & $Python -m unittest discover -s test/python -p test_xlm_alignment.py }
Invoke-Checked { & $Python test/python/xlm_reference.py }
if (!$SkipBuild) { Invoke-Checked { dotnet build tools/GemmaConformance -c Release --tl:off --nologo -v minimal } }
Invoke-Checked { dotnet tools/GemmaConformance/bin/Release/net10.0/GemmaConformance.dll --xlm test/Fixture/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model artifacts/xlm/reference.json artifacts/xlm/actual.json }
Invoke-Checked { & $Python test/python/xlm_reference.py --verify artifacts/xlm/actual.json }

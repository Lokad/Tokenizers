$ErrorActionPreference = 'Stop'
$taskGenerated = Join-Path $PSScriptRoot '../src/Vocab/proto/SentencepieceModel.cs'
$taskSource = [IO.File]::ReadAllText($taskGenerated)
$taskCandidate = [IO.Path]::GetTempFileName()
try {
    # Cross-platform newline tolerance is intentional; invisible string content is not.
    [IO.File]::WriteAllText($taskCandidate, $taskSource.Replace("`r`n", "`n").Replace("`n", "`r`n"))
    & (Join-Path $PSScriptRoot 'generate-protobuf.ps1') -Check -CheckFile $taskCandidate
    $taskDescriptor = $taskSource.IndexOf('byte[] descriptorData', [StringComparison]::Ordinal)
    if ($taskDescriptor -lt 0) { throw 'Descriptor data anchor missing' }
    $taskLiteral = [regex]::Match($taskSource.Substring($taskDescriptor), '"([A-Za-z0-9+/=]+)"')
    if (!$taskLiteral.Success) { throw 'Descriptor literal missing' }
    $taskPosition = $taskDescriptor + $taskLiteral.Groups[1].Index + 3
    [IO.File]::WriteAllText($taskCandidate, $taskSource.Insert($taskPosition, ([char]0x200B).ToString()))
    $taskRejected = $false
    try { & (Join-Path $PSScriptRoot 'generate-protobuf.ps1') -Check -CheckFile $taskCandidate }
    catch {
        if (!$_.Exception.Message.Contains('Generated protobuf C# differs', [StringComparison]::Ordinal)) { throw }
        $taskRejected = $true
    }
    if (!$taskRejected) { throw 'Invisible descriptor corruption passed canonical generation' }
    Write-Host 'PASS canonical newline control and invisible descriptor-corruption rejection'
} finally { Remove-Item -LiteralPath $taskCandidate }

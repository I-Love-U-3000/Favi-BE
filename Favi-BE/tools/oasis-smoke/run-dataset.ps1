param(
    [string] $Config = 'tools/oasis-smoke/configs/benchmark.json',
    [string] $Output,
    [string] $Python = '.tools/oasis-env/Scripts/python.exe',
    [Nullable[int]] $Seed,
    [ValidateRange(60, 14400)] [int] $TimeoutSeconds = 1800,
    [switch] $VerifyFavi
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $solutionRoot
try {
    if (-not (Test-Path -LiteralPath $Python -PathType Leaf)) {
        throw 'Missing Python environment. Follow document/DATA_GENERATION_FLOW.md setup.'
    }
    if (-not $Output) { $Output = Join-Path '.tools/datasets' ([Guid]::NewGuid().ToString('N')) }
    $arguments = @('-X', 'utf8', 'tools/oasis-smoke/generate_dataset.py', '--config', $Config, '--output', $Output, '--timeout-seconds', $TimeoutSeconds.ToString())
    if ($null -ne $Seed) { $arguments += @('--seed', $Seed.ToString()) }
    & $Python @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Dataset generation failed; inspect failure.json and raw evidence.' }
    if ($VerifyFavi) {
        & (Join-Path $PSScriptRoot 'verify.ps1') -ExistingRunDirectory $Output -Python $Python
    }
    Write-Output ('Dataset ready: ' + (Resolve-Path -LiteralPath $Output).Path)
}
finally { Pop-Location }

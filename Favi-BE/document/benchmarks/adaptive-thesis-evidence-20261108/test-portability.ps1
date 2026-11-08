param([Parameter(Mandatory)][string]$Output)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$destination = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $destination) { throw 'Test output must not exist' }
$null = New-Item -ItemType Directory -Path $destination
$benchmarks = Split-Path $PSScriptRoot -Parent
$inputs = @(
    'adaptive-confirmatory-ranking-20261108/summary.json',
    'adaptive-replay-20261108/summary.json',
    'adaptive-cost-audit-20261108/summary.json',
    'adaptive-http-20261108/summary.json',
    'adaptive-serving-export-20261108/provenance.json'
)
$checks = [Collections.Generic.List[string]]::new()
foreach ($style in @('LF', 'CRLF', 'BOM-CRLF')) {
    $root = Join-Path $destination $style
    $package = Join-Path $root 'adaptive-thesis-evidence-20261108'
    $null = New-Item -ItemType Directory -Path $package -Force
    foreach ($name in @('build-evidence.ps1', 'TABLES_VI.md')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $package
    }
    foreach ($relative in $inputs) {
        $target = Join-Path $root $relative
        $null = New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force
        $content = [IO.File]::ReadAllText((Join-Path $benchmarks $relative)).Replace("`r`n", "`n")
        if ($style -ne 'LF') { $content = $content.Replace("`n", "`r`n") }
        [IO.File]::WriteAllText($target, $content, [Text.UTF8Encoding]::new($style -eq 'BOM-CRLF'))
    }
    & (Join-Path $package 'build-evidence.ps1') -Check
    $checks.Add("PASS $style checkout")
}
$package = Join-Path $destination 'LF/adaptive-thesis-evidence-20261108'
$target = Join-Path $destination 'LF/adaptive-serving-export-20261108/provenance.json'
# Valid JSON with changed non-EOL whitespace must still be detected by the hash.
[IO.File]::AppendAllText($target, ' ', [Text.UTF8Encoding]::new($false))
$rejected = $false
try { & (Join-Path $package 'build-evidence.ps1') -Check }
catch { if ($_.Exception.Message -notmatch 'Generated tables drift') { throw }; $rejected = $true }
if (-not $rejected) { throw 'Changed source text was accepted' }
$checks.Add('PASS altered source text rejected')
$target = Join-Path $destination 'CRLF/adaptive-confirmatory-ranking-20261108/summary.json'
$json = Get-Content $target -Raw | ConvertFrom-Json
$json.scenarios.main.aggregate.R.ndcg.mean += 0.01
$json | ConvertTo-Json -Depth 100 | Set-Content $target
$rejected = $false
try { & (Join-Path $destination 'CRLF/adaptive-thesis-evidence-20261108/build-evidence.ps1') -Check }
catch { if ($_.Exception.Message -notmatch 'Mismatch: main/R mean') { throw }; $rejected = $true }
if (-not $rejected) { throw 'Changed numerical result was accepted' }
$checks.Add('PASS altered metric rejected')
$checks | Set-Content (Join-Path $destination 'results.txt')
$checks

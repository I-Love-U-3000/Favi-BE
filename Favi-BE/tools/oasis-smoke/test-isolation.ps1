# Real Docker regression: failing a new runner must not tear down an existing run.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
$owned = $false
$isolationLock = $null
$compose = @('compose', '--project-name', 'favi-oasis-smoke', '-f', 'tools/oasis-smoke/compose.yml')
function Get-SentinelIds {
    $ids = @(& docker @compose ps -q | Sort-Object)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect sentinel' }
    if ($ids.Count -ne 2) { throw 'Sentinel containers were stopped or removed' }
    return ($ids -join ',')
}
try {
    New-Item -ItemType Directory -Path '.tools' -Force | Out-Null
    $isolationLock = [IO.File]::Open((Join-Path $root '.tools/oasis-smoke-isolation.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $preexisting = @(& docker ps -a --filter 'label=com.docker.compose.project=favi-oasis-smoke' --format '{{.ID}}')
    if ($LASTEXITCODE -ne 0 -or $preexisting.Count -ne 0) { throw 'Legacy project already exists; refusing to touch it' }
    $owned = $true
    & docker @compose up -d --wait --wait-timeout 60
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start sentinel' }
    $before = Get-SentinelIds
    $failed = $false
    try { & "$PSScriptRoot/verify.ps1" -Python '.tools/nonexistent-python-for-isolation-test.exe' }
    catch { $failed = $true }
    if (-not $failed -or (Get-SentinelIds) -ne $before) { throw 'Early failure cleanup touched existing project' }
    Write-Output 'PASS missing Python fails without stopping existing Compose project'
    $failed = $false
    try { & "$PSScriptRoot/verify.ps1" }
    catch { $failed = $true }
    if (-not $failed -or (Get-SentinelIds) -ne $before) { throw 'Port conflict cleanup touched existing project' }
    $report = Get-Content '.tools/oasis-smoke/latest.json' -Raw | ConvertFrom-Json
    if ($report.passed -or $report.composeAttempted) { throw 'Port conflict must fail before starting Compose' }
    Write-Output 'PASS occupied ports fail before Compose; existing container IDs remain unchanged'
}
finally {
    try {
        if ($owned) {
            & docker @compose down
            if ($LASTEXITCODE -ne 0) { throw 'Sentinel cleanup failed' }
        }
    }
    finally { if ($isolationLock) { $isolationLock.Dispose() }; Pop-Location }
}

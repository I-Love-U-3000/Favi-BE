$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
# Hold the old fixed API port. The simulator gate must use its own Kestrel address
# and must never even connect to this unrelated listener.
$unrelated = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 18191)
try {
    $unrelated.Start()
    & "$PSScriptRoot/verify.ps1" -Simulator -Python '.tools/oasis-env/Scripts/python.exe'
    $run = Get-Content '.tools/oasis-smoke/latest.json' -Raw | ConvertFrom-Json
    $checks = Get-Content (Join-Path $run.runDirectory 'report/checks.json') -Raw | ConvertFrom-Json
    if (-not $run.passed -or -not $checks.passed -or ([Uri]$checks.apiUrl).Port -eq 18191) { throw 'Gate did not use an owned dynamic API address' }
    if ($unrelated.Pending()) { throw 'Runner connected to unrelated API listener' }
    Write-Output 'PASS full OASIS gate uses child-owned random API port; unrelated listener received no connections'
}
finally { $unrelated.Stop(); Pop-Location }

param([string] $Python = '.tools/python/cpython-3.11.14-windows-x86_64-none/python.exe', [switch] $Simulator, [string] $ExistingRunDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $solutionRoot
$runId = [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path '.tools/oasis-smoke' $runId
$composeProject = 'favi-oasis-' + $runId
$composeAttempted = $false
$report = @{ passed = $false; source = 'synthetic OASIS SQLite fixture'; startedUtc = [DateTime]::UtcNow.ToString('o') }
if ($Simulator) { $report.source = 'upstream OASIS engine, ManualAction, no LLM inference' }
function Invoke-Check([string] $Executable, [string[]] $Arguments) {
    if ($Executable -eq 'docker') {
        $dockerStart = [Diagnostics.ProcessStartInfo]::new('docker')
        $dockerStart.UseShellExecute = $false
        $dockerStart.CreateNoWindow = $true
        foreach ($dockerArgument in $Arguments) { $dockerStart.ArgumentList.Add($dockerArgument) }
        $dockerProcess = [Diagnostics.Process]::Start($dockerStart)
        try {
            if (-not $dockerProcess.WaitForExit(90000)) {
                $dockerProcess.Kill($true)
                $dockerProcess.WaitForExit()
                throw 'Docker command timed out after 90 seconds; check Docker Desktop engine'
            }
            if ($dockerProcess.ExitCode -ne 0) { throw "Docker command failed (exit $($dockerProcess.ExitCode))" }
        }
        finally { $dockerProcess.Dispose() }
        return
    }
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Failed: $Executable $($Arguments -join ' ') (exit $LASTEXITCODE)" }
}
try {
    if ($Simulator -and $ExistingRunDirectory) { throw 'Choose Simulator or ExistingRunDirectory' }
    if (-not (Test-Path -LiteralPath $Python)) { throw 'Pass -Python with a working Python 3.10+ executable path' }
    $report['revision'] = (& git rev-parse HEAD).Trim()
    Invoke-Check $Python @('-m', 'unittest', 'discover', '-s', 'tools/oasis_adapter', '-p', 'test_*.py', '-v')
    Invoke-Check $Python @('-m', 'unittest', 'discover', '-s', 'tools/oasis-smoke', '-p', 'test_*.py', '-v')
    if ($ExistingRunDirectory) {
        $existingRoot = (Resolve-Path -LiteralPath $ExistingRunDirectory).Path
        $report.source = 'existing snapshot; see original provenance for simulation mode'
        $report['sourceRunDirectory'] = $existingRoot
        New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $existingRoot 'export') -Destination (Join-Path $runRoot 'export') -Recurse
    }
    elseif ($Simulator) {
        Invoke-Check $Python @('tools/oasis-smoke/run_simulator.py', $runRoot, '.tools/oasis-upstream')
    }
    else { Invoke-Check $Python @('tools/oasis-smoke/prepare_fixture.py', $runRoot) }
    Invoke-Check 'dotnet' @('build', 'tools/oasis-smoke/OasisSmoke.csproj', '-m:1', '-nodeReuse:false', '-v:quiet')
    # Fixed loopback ports are intentionally serial. Fail before touching Compose
    # if any are busy. A bind race still only cleans this run's unique project.
    foreach ($port in @(15449, 16399)) {
        $probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $port)
        try { $probe.Start() }
        finally { $probe.Stop() }
    }
    $composeAttempted = $true
    Invoke-Check 'docker' @('compose', '--project-name', $composeProject, '-f', 'tools/oasis-smoke/compose.yml', 'up', '-d', '--wait', '--wait-timeout', '60')
    Invoke-Check 'dotnet' @('tools/oasis-smoke/bin/Debug/net10.0/OasisSmoke.dll', (Join-Path $runRoot 'export'))
    $report.passed = $true
}
catch { $report['error'] = $_.Exception.Message; throw }
finally {
    if ($composeAttempted) {
        try { Invoke-Check 'docker' @('compose', '--project-name', $composeProject, '-f', 'tools/oasis-smoke/compose.yml', 'down') }
        catch { $report.passed = $false; $report['cleanupError'] = $_.Exception.Message; Write-Warning $_.Exception.Message }
    }
    $report['finishedUtc'] = [DateTime]::UtcNow.ToString('o')
    $report['runDirectory'] = $runRoot
    $report['composeProject'] = $composeProject
    $report['composeAttempted'] = $composeAttempted
    New-Item -ItemType Directory -Path '.tools/oasis-smoke' -Force | Out-Null
    New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
    $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runRoot 'run.json') -Encoding utf8
    $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath '.tools/oasis-smoke/latest.json' -Encoding utf8
    Pop-Location
}
if (-not $report.passed) { throw 'OASIS integration did not pass' }

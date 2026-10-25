$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $solutionRoot
$reportPath = Join-Path $solutionRoot '.tools/cold-start-gate.json'
$steps = [System.Collections.Generic.List[string]]::new()
$report = @{ passed = $false; startedUtc = [DateTime]::UtcNow.ToString('o'); steps = $steps }
function Invoke-GateStep([string] $Executable, [string[]] $Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Gate failed: $Executable $($Arguments -join ' ') (exit $LASTEXITCODE)" }
    $steps.Add("$Executable $($Arguments -join ' ')")
}
try {
    New-Item -ItemType Directory -Path '.tools' -Force | Out-Null
    $revision = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source revision' }
    $report['revision'] = $revision.Trim()
    Invoke-GateStep 'git' @('diff', '--check')
    Invoke-GateStep 'dotnet' @('test', 'Favi-BE.UnitTests/Favi-BE.UnitTests.csproj', '-m:1', '-nodeReuse:false', '-v:quiet')
    Invoke-GateStep 'dotnet' @('test', 'Favi-BE.ArchitectureTests/Favi-BE.ArchitectureTests.csproj', '-m:1', '-nodeReuse:false', '-v:quiet')
    Invoke-GateStep 'dotnet' @('build', 'tools/cold-start-smoke/ColdStartSmoke.csproj', '-m:1', '-nodeReuse:false', '-v:quiet')
    Invoke-GateStep 'docker' @('compose', '-f', 'tools/cold-start-smoke/compose.yml', 'up', '-d', '--wait')
    Invoke-GateStep 'dotnet' @('tools/cold-start-smoke/bin/Debug/net10.0/ColdStartSmoke.dll', '--upgrade')
    Invoke-GateStep 'dotnet' @('tools/cold-start-smoke/bin/Debug/net10.0/ColdStartSmoke.dll', '--upgrade', '--existing-columns')
    $report.passed = $true
}
catch {
    $report['error'] = $_.Exception.Message
    throw
}
finally {
    try {
        Invoke-GateStep 'docker' @('compose', '-f', 'tools/cold-start-smoke/compose.yml', 'down')
    }
    catch {
        $report.passed = $false
        $report['cleanupError'] = $_.Exception.Message
        Write-Warning $_.Exception.Message
        $global:LASTEXITCODE = 1
    }
    $report['finishedUtc'] = [DateTime]::UtcNow.ToString('o')
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding utf8
    Pop-Location
}
if (-not $report.passed) { throw 'Cold-start gate did not pass; inspect .tools/cold-start-gate.json' }

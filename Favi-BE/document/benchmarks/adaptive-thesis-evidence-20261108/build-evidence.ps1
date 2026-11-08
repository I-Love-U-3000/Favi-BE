param([switch]$Check)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$culture = [Globalization.CultureInfo]::InvariantCulture
$root = Split-Path $PSScriptRoot -Parent
function Read-Json($relative) { Get-Content (Join-Path $root $relative) -Raw | ConvertFrom-Json }
function Mean($values) { ($values | Measure-Object -Average).Average }
function Near($actual, $expected, $label) {
    if ([Math]::Abs($actual - $expected) -gt 1e-12) { throw "Mismatch: $label" }
}
function F($value) { ([double]$value).ToString('F9', $culture) }
function Source-TextHash($path) {
    # Checkout-independent text fingerprint; original archive hashes stay byte-exact.
    $text = [IO.File]::ReadAllText($path, [Text.UTF8Encoding]::new($false, $true)).Replace("`r`n", "`n")
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($text)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}
$ranking = Read-Json 'adaptive-confirmatory-ranking-20261108/summary.json'
$replay = Read-Json 'adaptive-replay-20261108/summary.json'
$cost = Read-Json 'adaptive-cost-audit-20261108/summary.json'
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# Bảng số liệu sinh từ JSON đã khóa')
$lines.Add('')
$lines.Add('Sinh bằng build-evidence.ps1; -- không chạy model. nDCG@10 trung bình optimizer trong dataset trước, rồi trung bình đều ba dataset. Số hiển thị chín chữ số thập phân; JSON nguồn giữ độ chính xác gốc.')
$lines.Add('')
$lines.Add('## Chất lượng toàn tập có target')
$lines.Add('')
$lines.Add('| Scenario | Method | Mean nDCG@10 | Dataset sample std | Mean Recall@10 |')
$lines.Add('|---|---|---:|---:|---:|')
foreach ($scenario in @('main','sparse')) {
    $s = $ranking.scenarios.$scenario
    if ($s.datasets.Count -ne 3) { throw 'Expected three dataset seeds' }
    foreach ($method in @('popularity','topic-only','formula','formula-v2','ease','ppr','lightgcn','B1','R')) {
        $values = @($s.datasets | ForEach-Object { $_.methods.$method.all.ndcg.mean })
        Near (Mean $values) $s.aggregate.$method.ndcg.mean "$scenario/$method mean"
        $a = $s.aggregate.$method
        $lines.Add("| $scenario | $method | $(F $a.ndcg.mean) | $(F $a.ndcg.std) | $(F $a.recall.mean) |")
    }
    Near (Mean @($s.datasets | ForEach-Object { $_.methods.R.all.ndcg.mean - $_.methods.B1.all.ndcg.mean })) $s.delta_R_B1_ndcg.mean "$scenario paired delta"
}
$lines.Add('')
$lines.Add('## Chất lượng theo lịch sử: main')
$lines.Add('')
$lines.Add('C0/C1/C2: 0 positive, lần lượt 0/1/2–10 topics; T1: 1 positive; T2: 2–4; W1: 5–19; W2: từ 20. Đây là các nhóm người khác nhau tại cutoff400, không phải đường tiến bộ theo thời gian của cùng người.')
$lines.Add('')
$lines.Add('| Cohort | Target users seeds11/12/13 | B1 mean | R mean | R−B1 | Popularity mean | Topic-only mean |')
$lines.Add('|---|---|---:|---:|---:|---:|---:|')
foreach ($cohort in @('C0','C1','C2','T1','T2','W1','W2')) {
    $datasets = $ranking.scenarios.main.datasets
    $counts = @($datasets | ForEach-Object { $_.methods.B1.cohorts.$cohort.users }) -join '/'
    $means = @{}
    foreach ($method in @('B1','R','popularity','topic-only')) {
        $means[$method] = Mean @($datasets | ForEach-Object { $_.methods.$method.cohorts.$cohort.metrics.ndcg.mean })
    }
    $delta = $means.R - $means.B1
    Near $delta $ranking.scenarios.main.cohort_delta_R_B1_ndcg.$cohort.mean "cohort/$cohort"
    $lines.Add("| $cohort | $counts | $(F $means.B1) | $(F $means.R) | $(F $delta) | $(F $means.popularity) | $(F $means['topic-only']) |")
}
$lines.Add('')
$lines.Add('## Robustness main: cùng paired users, selection giữ nguyên')
$lines.Add('')
$lines.Add('| Mode | B1 mean | R mean | R−B1 |')
$lines.Add('|---|---:|---:|---:|')
foreach ($mode in @('primary','informative','noisy','independent','no-topics')) {
    $b = Mean @($replay.scenarios.main.datasets | ForEach-Object { $_.modes.$mode.B1.all.ndcg.mean })
    $r = Mean @($replay.scenarios.main.datasets | ForEach-Object { $_.modes.$mode.R.all.ndcg.mean })
    if ($mode -eq 'primary') { Near $r $ranking.scenarios.main.aggregate.R.ndcg.mean 'replay primary' }
    if ($mode -in @('noisy','independent','no-topics') -and $r -ge $b) { throw 'Expected retained negative robustness' }
    $lines.Add("| $mode | $(F $b) | $(F $r) | $(F ($r-$b)) |")
}
if ($ranking.main_practical_gate.passed) { throw 'Primary practical failure must remain visible' }
foreach ($name in @('original_search_seconds','original_validation_fit_only_seconds','original_training_peak_rss_bytes')) {
    if ($null -ne $cost.missing.$name) { throw "Historical missing measurement changed: $name" }
}
Near (Mean @($cost.runs | ForEach-Object { $_.elapsed_seconds })) ($cost.total_elapsed_seconds / 6) 'elapsed sum'
Near (($cost.runs.unique_refits.seconds | Measure-Object -Sum).Sum) $cost.total_unique_refit_seconds 'unique refits'
$lines.Add('')
$lines.Add('## Nguồn và SHA256 văn bản UTF-8/LF')
$lines.Add('')
$lines.Add('Hash văn bản chuẩn hóa UTF-8 không BOM và CRLF thành LF để kiểm cùng nội dung qua Git checkout; không parse/serialize lại JSON hoặc bỏ whitespace khác. Đây không phải hash bytes archive gốc. Hash bytes lịch sử giữ ở manifests/receipts. Raw archive nằm ở .tools; hash không xác nhận telemetry chưa được đo.')
$lines.Add('')
$lines.Add('| File tương đối từ document/benchmarks | SHA256 |')
$lines.Add('|---|---|')
foreach ($relative in @('adaptive-confirmatory-ranking-20261108/summary.json','adaptive-replay-20261108/summary.json','adaptive-cost-audit-20261108/summary.json','adaptive-http-20261108/summary.json','adaptive-serving-export-20261108/provenance.json')) {
    $hash = Source-TextHash (Join-Path $root $relative)
    $lines.Add("| $relative | $hash |")
}
$content = ($lines -join "`n") + "`n"
$target = Join-Path $PSScriptRoot 'TABLES_VI.md'
if ($Check) {
    if ((Get-Content $target -Raw).Replace("`r`n", "`n") -cne $content) { throw 'Generated tables drift; regenerate and review' }
    Write-Output 'PASS: paired arithmetic, 18 method means, seven cohort deltas, replay primary, negative robustness, missing cost measurements, source hashes and generated tables.'
} else { [IO.File]::WriteAllText($target, $content, [Text.UTF8Encoding]::new($false)) }

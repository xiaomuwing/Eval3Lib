#Requires -Version 5.1
<#
.SYNOPSIS
    新旧引擎同公式性能对照：跑 N 轮，两侧都取“最快一轮”，输出对照表。

.DESCRIPTION
    · 旧版：Windows PowerShell 加载遗留 VB 编译产物 bin\Debug\Eval3.dll（.NET Framework 4.8）
    · 新版：dotnet run --project src\Eval3.Benchmarks --parity（.NET 10）
    · 两侧公式 / 变量 / 函数库完全一致，先校验结果值一致，再比较耗时与分配
    之所以要多轮取最快：旧版每次解析分配约 10 万字节，GC 抖动会显著影响单次测量。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools\Compare-Performance.ps1 -Runs 3
#>
param(
    [int]$Runs = 3,
    [int]$ParseIterations = 20000,
    [int]$EvalIterations = 200000,
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\artifacts')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$legacyScript = Join-Path $PSScriptRoot 'Bench-LegacyVb.ps1'
$modernProject = Join-Path $root 'src\Eval3.Benchmarks'
$legacyCsv = Join-Path $OutputDir 'legacy-vb.csv'
$modernCsv = Join-Path $OutputDir 'eval3-net10.csv'

function Merge-BestRound {
    param($Rows)

    $Rows | Group-Object { "$($_.formula)|$($_.mode)" } | ForEach-Object {
        $_.Group | Sort-Object { [double]$_.ns_per_op } | Select-Object -First 1
    }
}

$legacyRows = @()
$modernRows = @()

for ($round = 1; $round -le $Runs; $round++) {
    Write-Host "第 $round / $Runs 轮…" -ForegroundColor Cyan
    & powershell -NoProfile -ExecutionPolicy Bypass -File $legacyScript `
        -OutFile $legacyCsv -ParseIterations $ParseIterations -EvalIterations $EvalIterations | Out-Null
    $legacyRows += Import-Csv $legacyCsv

    & dotnet run --project $modernProject -c Release -- --parity `
        --parse-iterations $ParseIterations --eval-iterations $EvalIterations --out $modernCsv | Out-Null
    $modernRows += Import-Csv $modernCsv
}

$legacy = Merge-BestRound $legacyRows
$modern = Merge-BestRound $modernRows

# 先校验两侧结果值一致
$valueMismatch = @()
foreach ($row in $legacy) {
    $peer = $modern | Where-Object { $_.formula -eq $row.formula -and $_.mode -eq $row.mode }
    if ($peer -and [string]$peer.value -ne [string]$row.value) {
        $valueMismatch += "$($row.formula)/$($row.mode)：旧 $($row.value) ≠ 新 $($peer.value)"
    }
}

if ($valueMismatch.Count -gt 0) {
    Write-Warning ("结果值不一致：" + ($valueMismatch -join '；'))
} else {
    Write-Host "结果值校验：两侧完全一致 ✓" -ForegroundColor Green
}

$table = foreach ($row in $legacy) {
    $peer = $modern | Where-Object { $_.formula -eq $row.formula -and $_.mode -eq $row.mode }
    if (-not $peer) { continue }

    $legacyNs = [double]$row.ns_per_op
    $modernNs = [double]$peer.ns_per_op
    [pscustomobject]@{
        公式     = $row.formula
        模式     = $row.mode
        旧版耗时 = '{0:N1} ns' -f $legacyNs
        新版耗时 = '{0:N1} ns' -f $modernNs
        加速     = '{0:N1}×' -f ($legacyNs / $modernNs)
        旧版分配 = '{0:N0} B' -f [double]$row.bytes_per_op
        新版分配 = '{0:N1} B' -f [double]$peer.bytes_per_op
        分配降幅 = '{0:N1}×' -f ([double]$row.bytes_per_op / [math]::Max([double]$peer.bytes_per_op, 0.01))
    }
}

$table | Format-Table -AutoSize

$modernExtra = $modern | Where-Object { $_.mode -in @('cached-eval', 'eval-fast') }
if ($modernExtra) {
    Write-Host "新版额外模式（旧版无对应能力）：" -ForegroundColor Yellow
    $modernExtra | ForEach-Object {
        [pscustomobject]@{
            公式 = $_.formula
            模式 = $_.mode
            耗时 = '{0:N1} ns' -f [double]$_.ns_per_op
            每秒 = '{0:N0}' -f [double]$_.ops_per_sec
            分配 = '{0:N1} B' -f [double]$_.bytes_per_op
        }
    } | Format-Table -AutoSize
}

$ratios = $table | ForEach-Object { [double]($_.加速 -replace '×', '') }
Write-Host ("平均加速：{0:N1}×（最小 {1:N1}×，最大 {2:N1}×）" -f `
    ($ratios | Measure-Object -Average).Average, ($ratios | Measure-Object -Minimum).Minimum, ($ratios | Measure-Object -Maximum).Maximum) `
    -ForegroundColor Green

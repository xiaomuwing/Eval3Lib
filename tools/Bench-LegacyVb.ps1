#Requires -Version 5.1
<#
.SYNOPSIS
    对遗留 VB 版引擎（bin\Debug\Eval3.dll）做性能取样，输出 CSV 供与新版本对照。

.DESCRIPTION
    · parse+eval：每次迭代都重新 Parse 再取值（调用方式与新版本的 Parse+Evaluate 对齐）
    · eval-only：解析一次后反复求值（与新版本的 Compile + Evaluate 对齐）
    变量、函数库、公式集合与 src\Eval3.Benchmarks 的 --parity 模式完全一致。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools\Bench-LegacyVb.ps1
#>
param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\Eval3.dll'),
    [string]$OutFile = (Join-Path $PSScriptRoot '..\artifacts\legacy-vb.csv'),
    [int]$ParseIterations = 20000,
    [int]$EvalIterations = 200000
)

$ErrorActionPreference = 'Stop'
$resolved = [System.IO.Path]::GetFullPath($AssemblyPath)
if (-not (Test-Path -LiteralPath $resolved)) {
    throw "找不到遗留程序集：$resolved"
}

Add-Type -Path $resolved
Add-Type -Path (Join-Path $PSScriptRoot 'LegacyProbe.cs') -ReferencedAssemblies $resolved

Write-Host "遗留程序集：$resolved" -ForegroundColor Cyan
Write-Host "运行时：$([System.Diagnostics.Stopwatch]::IsHighResolution) / CLR $([System.Environment]::Version)" -ForegroundColor DarkGray

[LegacyBench]::Run($ParseIterations, $EvalIterations, $OutFile)

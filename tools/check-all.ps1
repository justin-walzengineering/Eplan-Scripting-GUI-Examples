<#
  Compile-checks every example against each installed Eplan version.
  Usage:  pwsh tools/check-all.ps1            (auto-detects installed versions)
          pwsh tools/check-all.ps1 -Versions 2025.0.3,2026.0.3
  Local only. Reads the Eplan install by path; copies nothing.
#>
param(
    [string[]]$Versions,
    [string]$PlatformRoot = 'C:\Program Files\EPLAN\Platform'
)

$project = Join-Path $PSScriptRoot 'CompileCheck'
if (-not $Versions) {
    $Versions = Get-ChildItem $PlatformRoot -Directory | Where-Object { $_.Name -match '^\d{4}\.' } | Select-Object -ExpandProperty Name
}

$failed = $false
foreach ($v in $Versions) {
    $bin = Join-Path $PlatformRoot "$v\Bin"
    if (-not (Test-Path $bin)) { Write-Host "SKIP  $v (not found at $bin)"; continue }
    $result = & dotnet build $project -c Release --no-incremental "-p:EplanBin=$bin" -v:q -nologo 2>&1
    if ($LASTEXITCODE -eq 0) { Write-Host "PASS  Eplan $v" -ForegroundColor Green }
    else { Write-Host "FAIL  Eplan $v" -ForegroundColor Red; $result | Select-String 'error' | ForEach-Object { "      $_" }; $failed = $true }
}
if ($failed) { exit 1 }

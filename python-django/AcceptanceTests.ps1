param([switch]$Headful, [string]$Python = "python")
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$py = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
if (-not (Test-Path $py)) { $py = $Python }
& $py -m playwright install chromium
if ($LASTEXITCODE) { throw "Chromium installation failed" }
if ($Headful) { $env:HEADFUL = "1" }
& $py acceptance_tests.py
if ($LASTEXITCODE) { throw "Browser acceptance tests failed" }

param([switch]$Acceptance, [string]$Python = "python")
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$venv = Join-Path $PSScriptRoot ".venv"
if (-not (Test-Path (Join-Path $venv "Scripts/python.exe"))) { & $Python -m venv $venv; if ($LASTEXITCODE) { throw "venv creation failed" } }
$py = Join-Path $venv "Scripts/python.exe"
& $py -m pip install -r requirements.txt; if ($LASTEXITCODE) { throw "dependency installation failed" }
$env:DJANGO_DB_PATH = Join-Path $PSScriptRoot "build/private-build.sqlite3"
New-Item -ItemType Directory -Force (Split-Path $env:DJANGO_DB_PATH) | Out-Null
Remove-Item -Force -ErrorAction SilentlyContinue $env:DJANGO_DB_PATH
& $py manage.py check; if ($LASTEXITCODE) { throw "Django checks failed" }
& $py manage.py migrate --noinput; if ($LASTEXITCODE) { throw "database migrations failed" }
& $py manage.py test --verbosity 2; if ($LASTEXITCODE) { throw "Django tests failed" }
if ($Acceptance) { & $py -m playwright install chromium; if ($LASTEXITCODE) { throw "Chromium install failed" }; & $py acceptance_tests.py; if ($LASTEXITCODE) { throw "browser acceptance failed" } }

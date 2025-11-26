#!/usr/bin/env bash

# Bash version of build.bat
# Runs PrivateBuild.ps1 with pwsh (PowerShell Core)

set -e

pwsh -NoProfile -ExecutionPolicy Bypass -Command "& { ./PrivateBuild.ps1 -databaseName 'ChurchBulletin' $@; if (\$LASTEXITCODE -ne 0) { Write-Host 'ERROR: \$LASTEXITCODE' -ForegroundColor Red; exit \$LASTEXITCODE } }"

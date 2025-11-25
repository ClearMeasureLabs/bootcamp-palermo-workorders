#!/usr/bin/env bash

# Bash version of click_to_build.bat
# Runs PrivateBuild.ps1 with pwsh and pauses for user input

pwsh -NoProfile -ExecutionPolicy Bypass -Command "& { ./PrivateBuild.ps1 $@; if (\$LASTEXITCODE -ne 0) { Write-Host 'ERROR: \$LASTEXITCODE' -ForegroundColor Red; exit \$LASTEXITCODE } }"
exit_code=$?

if [ $exit_code -ne 0 ]; then
    echo "Build failed with exit code $exit_code"
fi

read -p "Press Enter to continue..."
exit $exit_code

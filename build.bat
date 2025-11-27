@echo off
setlocal

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& { .\PrivateBuild.ps1 -databaseName 'ChurchBulletin' %*; if ($lastexitcode -ne 0) {write-host 'ERROR: $lastexitcode' -fore RED; exit $lastexitcode} }"

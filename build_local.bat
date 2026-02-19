:: This is a copy of build.bat, but passing in local parameters for the developer workstation. 
:: parameters
::  -databaseServer - Your local SQL Server Instance, if not a default Instance

@echo off
setlocal

pwsh.exe -NoProfile -ExecutionPolicy Bypass -Command "& { .\PrivateBuild.ps1 -databaseServer '(LocalDb)\MSSQLLocalDB' %*; if ($lastexitcode -ne 0) {write-host 'ERROR: $lastexitcode' -fore RED; exit $lastexitcode} }"

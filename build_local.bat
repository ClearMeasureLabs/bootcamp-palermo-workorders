:: This is a copy of build.bat, but passing in local parameters for the developer workstation. 
:: parameters
::  -databaseServer - Your local SQL Server Instance, if not a default Instance
::  -migrateDbWithFlyway - Pass in $true if you want to run the Flyway migration demo. After AliaSQL creates the ChurchBulletin db, Flyway will add a column, as a test migration.

@echo off
setlocal

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& { .\PrivateBuild.ps1 -databaseServer "(LocalDb)\MSSQLLocalDB" -migrateDbWithFlyway $true %*; if ($lastexitcode -ne 0) {write-host 'ERROR: $lastexitcode' -fore RED; exit $lastexitcode} }"

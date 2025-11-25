#!/usr/bin/env bash

# Bash version of build_local.bat
# Runs PrivateBuild.ps1 with local parameters for developer workstation
# Parameters:
#  -databaseServer - A SQL Server instance name. Default is 'localhost'. 
#  -migrateDbWithFlyway - Pass in $true if you want to run the Flyway migration demo

pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1 -databaseServer 'localhost' -migrateDbWithFlyway $true "$@"
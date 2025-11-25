<#
.SYNOPSIS
    Tests the CI build process for the ChurchBulletin project.

.DESCRIPTION
    This script demonstrates the complete CI build process including:
    - Building all projects
    - Running unit tests
    - Running database migrations with DbUp
    - Running integration tests
    - Packaging artifacts
    
    The script can be run multiple times and will show that DbUp properly
    recognizes already-executed migration scripts.

.PARAMETER Clean
    If specified, drops and recreates the database before running the build.
    Use this for the first run or to start fresh.

.EXAMPLE
    .\TestCIBuild.ps1
    Runs the CI build with the existing database.

.EXAMPLE
    .\TestCIBuild.ps1 -Clean
    Drops the database, rebuilds it, then runs the CI build.

.NOTES
    This script was created to demonstrate the DbUp migration fixes that ensure:
    - Migrations are properly journaled in the SchemaVersions table
    - Integration tests preserve the migration journal
    - Builds can run repeatedly without database issues
#>

param(
    [switch]$Clean
)

# Import build functions
. .\BuildFunctions.ps1

$ErrorActionPreference = "Stop"

function Show-Header {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Show-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Show-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Yellow
}

function Check-DatabaseJournal {
    Show-Header "Checking Database Migration Journal"
    
    $result = sqlcmd -S "(LocalDb)\MSSQLLocalDB" -d "ChurchBulletin" -Q "SELECT COUNT(*) as Count FROM SchemaVersions" -h-1 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Show-Info "Database does not exist or journal table not found"
        return $false
    }
    
    $count = ($result | Select-String -Pattern "\d+" | Select-Object -First 1).Matches.Value
    
    if ($count -gt 0) {
        Show-Success "Found $count migration scripts in journal"
        return $true
    }
    else {
        Show-Info "Journal is empty"
        return $false
    }
}

function Reset-Database {
    Show-Header "Resetting Database"
    
    Show-Info "Dropping ChurchBulletin database..."
    $dropResult = sqlcmd -S "(LocalDb)\MSSQLLocalDB" -Q "ALTER DATABASE ChurchBulletin SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ChurchBulletin;" 2>&1
    
    if ($LASTEXITCODE -ne 0 -and $dropResult -notlike "*does not exist*") {
        Write-Host "Note: Database may not have existed" -ForegroundColor Gray
    }
    
    Show-Info "Running rebuild to create fresh database..."
    $databaseDll = Join-Path $PSScriptRoot "src\Database\bin\Release\net9.0\ClearMeasure.Bootcamp.Database.dll"
    
    if (-not (Test-Path $databaseDll)) {
        throw "Database.dll not found at $databaseDll. Please run 'dotnet build src/Database/Database.csproj -c Release' first."
    }
    
    & dotnet $databaseDll rebuild "(LocalDb)\MSSQLLocalDB" "ChurchBulletin" ".\src\Database\scripts"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Database rebuild failed"
    }
    
    Show-Success "Database rebuilt successfully"
}

function Run-CIBuild {
    Show-Header "Running CI Build"
    
    . .\build.ps1
    Invoke-CIBuild
    
    if ($LASTEXITCODE -ne 0) {
        throw "CI Build failed"
    }
    
    Show-Success "CI Build completed successfully"
}

# Main script execution
try {
    Show-Header "ChurchBulletin CI Build Test Script"
    Write-Host "Started at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n"
    
    if ($Clean) {
        Show-Info "Clean build requested - will reset database"
        Reset-Database
    }
    else {
        $journalExists = Check-DatabaseJournal
        
        if (-not $journalExists) {
            Show-Info "Database not initialized. Running with -Clean is recommended for first run."
            $response = Read-Host "Reset database now? (Y/N)"
            if ($response -eq 'Y' -or $response -eq 'y') {
                Reset-Database
            }
        }
    }
    
    # Check journal before build
    Write-Host ""
    Check-DatabaseJournal | Out-Null
    
    # Run the build
    Run-CIBuild
    
    # Check journal after build
    Write-Host ""
    Check-DatabaseJournal | Out-Null
    
    # Summary
    Show-Header "Build Summary"
    Show-Success "All steps completed successfully"
    Write-Host ""
    Show-Info "You can run this script again to verify repeatability"
    Show-Info "Each run should show 'No new scripts need to be executed' for migrations"
    Write-Host ""
    Write-Host "Completed at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
}
catch {
    Write-Host "`n❌ Build failed: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}

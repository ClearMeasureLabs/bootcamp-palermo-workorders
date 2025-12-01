# Porting Azure DevOps Pipeline to Octopus Deploy

## Overview

This guide explains how to port the Azure DevOps deployment stages to Octopus Deploy while keeping the build stages in CI/CD.

## Architecture Decision

**Recommended Approach: Hybrid**
- **CI (Azure DevOps/GitHub Actions)**: Build, test, package, publish artifacts
- **Octopus Deploy**: Deploy, run migrations, manage environments

## What to Port to Octopus Deploy

### ✅ Port These Stages:

#### 1. **Database Migration Steps** (TDD, UAT, PROD)
   - Download Database Package from NuGet feed
   - Run baseline schema
   - Run database migrations
   - Use existing scripts: `UpdateAzurePipelineSql.ps1`

#### 2. **Container App Deployment** (TDD, UAT, PROD)
   - Update Azure Container App with new image
   - Set environment variables (connection strings)
   - Already partially implemented in `deployment_process.ocl`

#### 3. **Acceptance Tests** (TDD only)
   - Download Acceptance Test package
   - Install Playwright
   - Run VSTest acceptance tests

#### 4. **Environment Promotion**
   - Use Octopus lifecycles for TDD → UAT → PROD promotion
   - Configure approval gates in Octopus

### ❌ Keep in CI/CD:

- Build and compilation
- Unit/Integration tests
- Code coverage publishing
- NuGet package creation
- Docker image building
- Pushing artifacts to feeds

## Octopus Deploy Implementation Plan

### Step 1: Database Migration Steps

Create deployment steps for database migrations:

```ocl
step "migrate-database" {
    name = "Migrate Database Schema"
    
    action {
        action_type = "Octopus.PowerShell"
        is_required = true
        properties = {
            Octopus.Action.Script.ScriptBody = <<-EOT
                $ErrorActionPreference = "Stop"
                
                # Get database parameters from Octopus variables
                $databaseServer = $OctopusParameters["DatabaseServer"]
                $databaseName = $OctopusParameters["DatabaseName"]
                $databaseUser = $OctopusParameters["DatabaseUser"]
                $databasePassword = $OctopusParameters["DatabasePassword"]
                $databaseAction = $OctopusParameters["DatabaseAction"]
                
                # Find the Database package (deployed as part of release)
                $databaseAssembly = Get-ChildItem -Path $OctopusParameters["Octopus.Action.Package[Database].ExtractedPath"] `
                    -Filter "ClearMeasure.Bootcamp.Database.dll" `
                    -Recurse | Select-Object -First 1 -ExpandProperty FullName
                
                $scriptDir = Join-Path $OctopusParameters["Octopus.Action.Package[Database].ExtractedPath"] "scripts"
                
                if (-not $databaseAssembly) {
                    throw "Could not find ClearMeasure.Bootcamp.Database.dll in Database package"
                }
                
                Write-Host "Executing database migration: $databaseAction"
                Write-Host "Database Server: $databaseServer"
                Write-Host "Database Name: $databaseName"
                
                # Run the database migration
                dotnet $databaseAssembly $databaseAction $databaseServer $databaseName $scriptDir $databaseUser $databasePassword
                
                if ($LASTEXITCODE -ne 0) {
                    throw "Database migration failed with exit code $LASTEXITCODE"
                }
                
                Write-Host "✅ Database migration completed successfully"
                EOT
            Octopus.Action.Script.ScriptSource = "Inline"
            Octopus.Action.Script.Syntax = "PowerShell"
        }
        
        # Package reference for Database package
        package {
            name = "Database"
            feed = "#{NuGetFeed}"
            acquisition_location = "Server"
        }
        
        worker_pool = "hosted-windows"
    }
}
```

### Step 2: Acceptance Test Step

Create step for running acceptance tests:

```ocl
step "run-acceptance-tests" {
    name = "Run Acceptance Tests"
    condition = "Success"
    requires_packages_to_be_acquired = true
    
    action {
        action_type = "Octopus.PowerShell"
        is_required = true
        properties = {
            Octopus.Action.Script.ScriptBody = <<-EOT
                $ErrorActionPreference = "Stop"
                
                $containerAppUrl = $OctopusParameters["ContainerAppUrl"]
                
                # Find acceptance test DLLs
                $testPath = $OctopusParameters["Octopus.Action.Package[AcceptanceTests].ExtractedPath"]
                $testDlls = Get-ChildItem -Path $testPath -Filter "*AcceptanceTests.dll" -Recurse
                
                if (-not $testDlls) {
                    throw "No acceptance test DLLs found in package"
                }
                
                # Install Playwright
                Write-Host "Installing Playwright..."
                $playwrightScript = Join-Path $testPath "playwright.ps1"
                if (Test-Path $playwrightScript) {
                    & pwsh $playwrightScript install --with-deps
                }
                
                # Run VSTest
                Write-Host "Running acceptance tests..."
                $vsTestPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
                
                if (-not (Test-Path $vsTestPath)) {
                    # Try Community edition
                    $vsTestPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
                }
                
                if (Test-Path $vsTestPath) {
                    $testFiles = ($testDlls | ForEach-Object { $_.FullName }) -join " "
                    & $vsTestPath $testFiles /Logger:trx /Logger:console
                    
                    if ($LASTEXITCODE -ne 0) {
                        throw "Acceptance tests failed with exit code $LASTEXITCODE"
                    }
                } else {
                    Write-Warning "VSTest not found, skipping acceptance tests"
                }
                
                Write-Host "✅ Acceptance tests completed"
                EOT
            Octopus.Action.Script.ScriptSource = "Inline"
            Octopus.Action.Script.Syntax = "PowerShell"
        }
        
        package {
            name = "AcceptanceTests"
            feed = "#{NuGetFeed}"
            acquisition_location = "Server"
        }
        
        worker_pool = "hosted-windows"
    }
}
```

### Step 3: Enhanced Container App Deployment

The existing `update-ui-gh-container-app` step already handles container app deployment. You may want to enhance it to:
- Pull from Docker registry instead of always using `:latest`
- Support different image tags per environment
- Add health checks

### Step 4: Deployment Process Structure

Recommended deployment process order:

```
1. Migrate Database
   ↓
2. Update Container App
   ↓
3. Run Acceptance Tests (TDD only)
```

### Step 5: Package References

You'll need to configure Octopus to:
1. **Download packages from NuGet feed** during deployment
2. Configure package feed in Octopus (point to Azure DevOps feed or GitHub Packages)
3. Set package version variables

### Step 6: Variables Needed

Add these variables to `.octopus/variables.ocl`:

```ocl
variable "DatabaseAction" {
    value "Update" {
        environment = ["tdd", "uat", "prod"]
    }
}

variable "ContainerAppUrl" {
    value "https://ui-gh-tdd.azurecontainerapps.io" {
        environment = ["tdd"]
    }
    value "https://ui-gh-uat.azurecontainerapps.io" {
        environment = ["uat"]
    }
    value "https://ui-gh-prod.azurecontainerapps.io" {
        environment = ["prod"]
    }
}

variable "NuGetFeed" {
    value "AzureDevOpsFeed" {
        environment = ["tdd", "uat", "prod"]
    }
}
```

## Migration Steps

1. **Create NuGet Feed in Octopus**
   - Go to Infrastructure → Feeds
   - Add Azure DevOps NuGet feed or GitHub Packages feed
   - Configure authentication

2. **Add Database Migration Step**
   - Create new step in deployment process
   - Reference Database package from feed
   - Use migration script

3. **Add Acceptance Test Step** (TDD only)
   - Create conditional step for TDD environment
   - Reference AcceptanceTests package
   - Configure VSTest execution

4. **Update Container App Step**
   - Enhance existing step if needed
   - Ensure proper image tagging

5. **Configure Lifecycle**
   - TDD → UAT → PROD
   - Add approval gates for UAT and PROD

6. **Update CI/CD Pipeline**
   - After build, push packages to feed
   - Trigger Octopus release creation
   - Pass package version to Octopus

## CI/CD Integration

In your Azure DevOps or GitHub Actions pipeline, after building and packaging:

```yaml
- name: Create Octopus Release
  uses: OctopusDeploy/create-release-action@v1
  with:
    api_key: ${{ secrets.OCTO_API_KEY }}
    server: ${{ secrets.OCTOPUS_URL }}
    space: ${{ vars.OCTOPUS_SPACE }}
    project: ${{ vars.OCTOPUS_PROJECT }}
    release_number: ${{ env.BUILD_BUILDNUMBER }}
    package_version: ${{ env.BUILD_BUILDNUMBER }}
    deploy_to: "TDD"
```

## Benefits of This Approach

✅ **Separation of Concerns**: Build in CI, Deploy in Octopus
✅ **Environment Management**: Octopus excels at environment-specific configuration
✅ **Approval Gates**: Built-in approval workflows for UAT/PROD
✅ **Deployment History**: Better tracking and rollback in Octopus
✅ **Configuration Management**: Environment-specific variables in one place
✅ **Reusability**: Same deployment process across environments

## Considerations

⚠️ **Build Stage**: Keep in CI/CD (Azure DevOps/GitHub Actions)
⚠️ **Package Feed**: Must be accessible from Octopus workers
⚠️ **Worker Requirements**: Workers need .NET runtime for database migrations
⚠️ **Testing**: Acceptance tests require VSTest on workers

## Next Steps

1. Create package feed in Octopus
2. Add database migration step
3. Enhance container app deployment step
4. Add acceptance test step for TDD
5. Configure lifecycle and approvals
6. Update CI pipeline to trigger Octopus releases


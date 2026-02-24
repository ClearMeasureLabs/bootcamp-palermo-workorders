# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. For solution layout and key paths, see also `.cursor/rules/codebase-structure.mdc`.

## Project Overview

This is a Work Order management system built with .NET 10.0, implementing Onion Architecture with Blazor WebAssembly UI, Entity Framework Core for data access, and deployed to Azure Container Apps.

## Solution Structure

**Solution:** `src/ChurchBulletin.sln`

| Project | Path | Role |
|---------|------|------|
| Core | `src/Core/` | Domain; no dependencies |
| DataAccess | `src/DataAccess/` | EF Core, MediatR handlers; refs Core only |
| Database | `src/Database/` | DB tooling, AliaSQL |
| UI.Server | `src/UI/Server/` | Blazor Server host, Lamar DI |
| UI.Client | `src/UI/Client/` | Blazor WASM |
| UI.Api | `src/UI/Api/` | Web API |
| UI.Shared | `src/UI.Shared/` | Shared UI |
| LlmGateway | `src/LlmGateway/` | Azure OpenAI / Ollama |
| ChurchBulletin.AppHost | `src/ChurchBulletin.AppHost/` | Aspire AppHost |
| ChurchBulletin.ServiceDefaults | `src/ChurchBulletin.ServiceDefaults/` | Aspire defaults |
| UnitTests | `src/UnitTests/` | NUnit + Shouldly |
| IntegrationTests | `src/IntegrationTests/` | NUnit, LocalDB |
| AcceptanceTests | `src/AcceptanceTests/` | NUnit + Playwright |

## Build and Test Commands

### Build
```powershell
# Quick build
.\build.bat

# Private build (local development - includes clean, compile, unit tests, DB migration, integration tests)
.\build.ps1 ; Invoke-PrivateBuild

# CI build (includes Invoke-PrivateBuild + packaging)
.\build.ps1 ; Invoke-CIBuild

# Using dotnet CLI directly
dotnet build src/ChurchBulletin.sln --configuration Release
```

### Testing
```powershell
# Run unit tests
cd src/UnitTests
dotnet test --configuration Release

# Run integration tests
cd src/IntegrationTests
dotnet test --configuration Release

# Run acceptance tests (requires Playwright browsers installed)
cd src/AcceptanceTests
pwsh bin/Debug/net10.0/playwright.ps1 install
dotnet test --configuration Debug
```

### Database Migration
```powershell
# Local database migration using AliaSQL
$databaseServer = "(LocalDb)\MSSQLLocalDB"
$databaseName = "ChurchBulletin"
MigrateDatabaseLocal -databaseServerFunc $databaseServer -databaseNameFunc $databaseName

# Direct AliaSQL execution
src/Database/scripts/AliaSQL.exe Rebuild (LocalDb)\MSSQLLocalDB ChurchBulletin src/Database/scripts
```

### Run Application Locally
```bash
cd src/UI/Server
dotnet run
# Application runs on https://localhost:7174
# Health check: https://localhost:7174/_healthcheck
```

## Onion Architecture Implementation

The solution follows strict Onion Architecture with dependency flow inward:

### Core (Inner Layer - No Dependencies)
- **Location**: `src/Core/`
- **Purpose**: Domain models, domain services interfaces, query objects
- **Key Types**: `WorkOrder`, `Employee`, `WorkOrderStatus`, `Role`
- **Pattern**: Uses MediatR for CQRS queries
- **Rule**: Core must not reference any other project

### DataAccess (Depends on Core Only)
- **Location**: `src/DataAccess/`
- **Purpose**: Entity Framework Core implementation, MediatR query/command handlers
- **Technology**: EF Core 10.0 with SQL Server
- **Key Components**: `DataContext`, mapping files, health checks
- **Rule**: Only references Core project

### UI Layer (Outer Layer)
- **UI.Server** (`src/UI/Server/`): Blazor Server hosting, dependency injection via Lamar, health checks aggregation
- **UI.Client** (`src/UI/Client/`): Blazor WebAssembly frontend
- **UI.Api** (`src/UI/Api/`): Web API endpoints (minimal dependencies)
- **UI.Shared** (`src/UI.Shared/`): Shared components

### Database Management
- **Database** (`src/Database/`): AliaSQL-based migrations with numbered scripts in `scripts/Update/` (001, 003, 004, etc.)

### Additional Layers
- **LlmGateway** (`src/LlmGateway/`): Azure OpenAI and Ollama integration for AI agent functionality

## Testing Structure

### Unit Tests (`src/UnitTests/`)
- **Framework**: NUnit 4.x
- **Test Data**: AutoBogus for generation
- **UI Testing**: bUnit for Blazor components
- **Naming**: `[MethodName]_[Scenario]_[ExpectedResult]` pattern
- **Assertions**: Use Shouldly framework
- **Test Doubles**: Prefix with "Stub" (e.g., `StubClass`), not "Mock"
- **Structure**: AAA pattern (Arrange, Act, Assert) without comments

### Integration Tests (`src/IntegrationTests/`)
- **Framework**: NUnit
- **Base Classes**: `IntegratedTestBase.cs`, `TestHost.cs`
- **Database**: Uses LocalDB with `TestDatabaseConfiguration.cs`, `DatabaseEmptier.cs`
- **Data Loading**: `ZDataLoader.cs` for test data

### Acceptance Tests (`src/AcceptanceTests/`)
- **Framework**: NUnit + Playwright
- **Browser Automation**: Microsoft.Playwright.NUnit 1.54.0
- **Base Classes**: `AcceptanceTestBase.cs`, `ServerFixture.cs`
- **Test Areas**: App/, AIAgents/, Authentication/, WorkOrders/

## Key Architectural Patterns

### CQRS with MediatR
- Queries in `Core/Queries/` (e.g., `EmployeeByUserNameQuery`, `WorkOrderByNumberQuery`)
- Commands via `IStateCommand` interface
- Handlers distributed across DataAccess and UI layers
- `IBus` interface wrapping MediatR for abstraction

### Dependency Injection
- **Container**: Lamar (StructureMap successor)
- **Registry**: `UIServiceRegistry.cs` for service registration
- **Scanning**: Automatic registration of handlers and services

### Health Checks
- `CanConnectToDatabaseHealthCheck` (DataAccess)
- `CanConnectToLlmServerHealthCheck` (LlmGateway)
- Custom health checks in Server project

## Coding Standards (from .github/copilot-instructions.md)

### Architecture
- Follow Onion Architecture principles strictly
- Keep business logic in Core project
- Data access isolated in DataAccess
- Do not add NuGet packages or project references without approval
- Keep existing .NET SDK and library versions unless specifically instructed to upgrade

### Naming and Style
- PascalCase for classes/methods, camelCase for variables
- XML documentation for public APIs
- Methods should be small and focused on single responsibility
- Use nullable reference types appropriately

### Testing
- All tests use Shouldly framework for assertions
- Follow AAA pattern without adding AAA comments
- Prefix test methods with "Should" or "When"
- Test doubles named with "Stub" prefix, not "Mock"

### Response Guidelines
- Do not anthropomorphize or use "I", "me", "you", "we", "us"
- No 2nd person pronouns
- Short, terse responses
- Example: Say "Checking this file" not "Let me check this file"

## Database Migrations

### Adding New Migrations (AliaSQL)
1. Create numbered script in `src/Database/scripts/Update/`
2. Use next sequential number (e.g., if 004 exists, create 005)
3. Script naming: `###_Description.sql`
4. Run Invoke-PrivateBuild to apply locally

### Migration Actions
- `Create`: Create new database
- `Update`: Apply incremental migrations
- `Rebuild`: Drop and recreate
- `TestData`: Load test data

## CI/CD Pipeline

### Pipeline File
`src/pure-azdo-pipeline.yml`

### Stages
1. **Integration_Build**: Build, test, package (pushes to Azure Artifacts)
2. **Docker Build & Push**: Build and push to Azure Container Registry
3. **TDD**: Auto-deploy, migrate DB, run acceptance tests
4. **UAT**: Manual approval required (any person can approve), deploy
5. **PROD**: Manual approval required, deploy

### Versioning
Format: `{major}.{minor}.{Rev:r}` (currently 1.4.x)

## Docker

### Build Container
```bash
# Requires pre-built artifacts in /built/ directory
docker build -t churchbulletin-ui .

# Run container
docker run -p 8080:8080 -p 80:80 churchbulletin-ui
```

### Base Image
`mcr.microsoft.com/dotnet/aspnet:10.0`

## Technology Stack

- **.NET**: 10.0
- **UI**: Blazor WebAssembly + Server
- **Data Access**: Entity Framework Core 10.0
- **Database**: SQL Server (Azure SQL in production)
- **CQRS**: MediatR
- **DI Container**: Lamar
- **Testing**: NUnit 4.x, bUnit, Playwright, Shouldly
- **AI/LLM**: Azure OpenAI, Ollama
- **Deployment**: Azure Container Apps, Azure DevOps Pipelines

## Architecture Documentation

- **Cursor rules:** `.cursor/rules/codebase-structure.mdc` (solution layout, key paths), `.cursor/rules/cloud-agent-instructions.mdc` (scope, workflow).
- **Copilot:** `.github/copilot-instructions.md`, `.github/copilot-code-review-instructions.md`.

PlantUML diagrams in `arch/`:
- `arch-c4-system.puml`: System context
- `arch-c4-container-deployment.puml`: Container deployment
- `arch-c4-component-project-dependencies.puml`: Project dependencies
- `arch-c4-class-domain-model.puml`: Domain model (WorkOrder, Employee, WorkOrderStatus, Role)

## Branch Naming Convention

All branches must be created inside a folder matching the username of the account creating the branch. The format is `{username}/{branch-description}`.

- For user `jeffreypalermo`, branches go under `jeffreypalermo/` (e.g., `jeffreypalermo/fix-work-order-status`)
- For user `johnsmith`, branches go under `johnsmith/` (e.g., `johnsmith/add-employee-search`)
- For AI agents (Claude, Copilot, Cursor), use the username of the account that initiated the session

## Important Notes

- **No Nuget packages**: Do not add new NuGet packages or change SDK versions without explicit approval
- **Onion Architecture**: Strictly maintain dependency rules (Core has no dependencies, DataAccess only references Core)
- **Test-after approach**: Generate code first, then implement tests
- **Shouldly assertions**: Use Shouldly for all test assertions, not FluentAssertions or Assert.That
- **Test naming**: Use "Stub" prefix for test doubles, never "Mock"
- Use TABS when generating new *.sql database migration scripts
- when on a branch, add/commit/push automatically without asking
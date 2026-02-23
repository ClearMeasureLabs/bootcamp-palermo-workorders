# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. For solution layout and key paths, see also `.cursor/rules/codebase-structure.mdc`.

## Project Overview

This is a Work Order management system built with .NET 10.0, implementing Onion Architecture with Blazor WebAssembly UI, Entity Framework Core for data access, and deployed to Azure Container Apps.

## Solution Structure

**Solution:** `src/ChurchBulletin.sln`

| Project | Path | Role |
|---------|------|------|
| Core | `src/Core/` | Domain models, interfaces, queries; no dependencies |
| DataAccess | `src/DataAccess/` | EF Core, MediatR handlers; refs Core only |
| Database | `src/Database/` | AliaSQL migrations and DB tooling |
| UI.Server | `src/UI/Server/` | Blazor Server host, Lamar DI, health checks aggregation |
| UI.Client | `src/UI/Client/` | Blazor WebAssembly frontend |
| UI.Api | `src/UI/Api/` | REST API endpoints (minimal dependencies) |
| UI.Shared | `src/UI.Shared/` | Shared UI components and models |
| Worker | `src/Worker/` | Background worker service (NServiceBus endpoints, messaging) |
| LlmGateway | `src/LlmGateway/` | Azure OpenAI / Ollama integration for AI agent functionality |
| ChurchBulletin.AppHost | `src/ChurchBulletin.AppHost/` | Aspire AppHost for orchestration |
| ChurchBulletin.ServiceDefaults | `src/ChurchBulletin.ServiceDefaults/` | Aspire service defaults and configuration |
| UnitTests | `src/UnitTests/` | NUnit 4.x, Shouldly, bUnit for Blazor components |
| IntegrationTests | `src/IntegrationTests/` | NUnit, LocalDB, database integration tests |
| AcceptanceTests | `src/AcceptanceTests/` | NUnit + Playwright, end-to-end browser tests |

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

**CRITICAL QUALITY GATES** (must pass before commit/PR):

```powershell
# Full private build (unit + integration tests, includes DB migration)
.\privatebuild.ps1

# Acceptance tests (full system, browser-based, run BEFORE marking PR ready)
.\acceptancetests.ps1

# Individual test runs
cd src/UnitTests
dotnet test --configuration Release

cd src/IntegrationTests
dotnet test --configuration Release

cd src/AcceptanceTests
# First time: Install Playwright browsers
pwsh bin/Debug/net10.0/playwright.ps1 install

# Run all acceptance tests
dotnet test --configuration Debug

# Run specific test class
dotnet test --filter "FullyQualifiedName~WorkOrderManageTests" --configuration Debug
```

**Workflow**:
1. Make code changes
2. Run `.\privatebuild.ps1` → must pass
3. Run `.\acceptancetests.ps1` → must pass
4. Commit and push
5. Create PR
6. Run `gh pr ready` when all tests pass

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

**Option 1: Direct Server Run**
```bash
cd src/UI/Server
dotnet run
# Application runs on https://localhost:7174
# Health check: https://localhost:7174/_healthcheck
```

**Option 2: Aspire Dashboard (Recommended for Development)**
```powershell
# Run Aspire AppHost orchestration
cd src/ChurchBulletin.AppHost
dotnet run
# Aspire Dashboard: https://localhost:17360
# Manages all services: UI.Server, Worker, LlmGateway, etc.
# Provides unified logging and diagnostics
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

### Background Worker Service
- **Worker** (`src/Worker/`): NServiceBus-based background worker service for asynchronous messaging
  - Uses `ClearMeasureLabs.HostedEndpoint.SqlServerTransport` for SQL Server-based message transport
  - Handles long-running operations and event processing
  - Sagas for multi-step processes
  - Message handlers for processing domain events
  - Coordinates with DataAccess for database operations

### Additional Layers
- **LlmGateway** (`src/LlmGateway/`): Azure OpenAI and Ollama integration for AI agent functionality
  - Abstracts LLM interactions
  - Health check: `CanConnectToLlmServerHealthCheck`

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
- **Setup**: Run `pwsh bin/Debug/net10.0/playwright.ps1 install` first (one-time, installs browsers)
- **Execution**: `dotnet test --configuration Debug` from `src/AcceptanceTests/` directory
- **Filtering**: `dotnet test --filter "FullyQualifiedName~TestClassName"` to run specific tests
- **Key Methods**: `LoginAsCurrentUser()`, `CreateAndSaveNewWorkOrder()`, `Click(testId)`, `Input(testId, value)`, `Select(testId, value)`, `Expect(locator)`

## LLM Integration (AI Agent Capabilities)

### LlmGateway Project
- **Purpose**: Abstracts LLM interactions for the application
- **Supported Providers**:
  - Azure OpenAI (production)
  - Ollama (local development)
- **Key Features**:
  - Configuration-driven provider selection
  - Health check monitoring
  - Integration with background Worker service for async AI operations
- **Usage**: Called from UI.Server, DataAccess handlers, and Worker for AI-powered features

### Configuration
- Environment variables: `LlmProvider` (AzureOpenAI or Ollama)
- Azure credentials via Managed Identity or connection strings
- Ollama local endpoint: typically `http://localhost:11434`

## Key Architectural Patterns

### CQRS with MediatR (In-Process & Distributed)
- **Queries**: Located in `Core/Queries/` (e.g., `EmployeeByUserNameQuery`, `WorkOrderByNumberQuery`)
  - Synchronous, read-only operations
  - Handlers in DataAccess layer
- **Commands**: Via `IStateCommand` interface
  - State changes and business operations
  - Handlers distributed across DataAccess and UI layers
- **Bus Abstraction**: `IBus` interface wrapping MediatR
- **Domain Events**: Published to NServiceBus for asynchronous processing
  - Processed by Worker service handlers
  - Enable loose coupling between services

### Messaging (NServiceBus)
- **Transport**: SQL Server (configured in Worker project)
- **Patterns**: Message handlers, sagas for orchestration
- **Location**: `src/Worker/Handlers/`, `src/Worker/Sagas/`
- **Integration**: Worker service automatically subscribes to domain events
- **Async Processing**: Long-running operations, notifications, external integrations

### Dependency Injection
- **Container**: Lamar (StructureMap successor)
- **Registry**: `UIServiceRegistry.cs` for service registration
- **Scanning**: Automatic registration of handlers and services

### Health Checks
- **Database**: `CanConnectToDatabaseHealthCheck` (DataAccess) - verifies SQL Server connectivity
- **LLM**: `CanConnectToLlmServerHealthCheck` (LlmGateway) - verifies Azure OpenAI/Ollama connectivity
- **Aggregation**: Server project aggregates all health checks
- **Endpoint**: `/healthcheck` on main application
- **Integration**: Monitored by Azure App Insights and Octopus for alerting
- **Runbook Integration**: Unhealthy app alerts trigger Octopus runbooks (configured via `OctoRunbookName`)

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

## Git Workflow for AI Assistants

### Branch Management
- **Branch Format**: `{username}/{branch-description}` (e.g., `claude/add-feature-ABC`)
- **AI Agents**: Use the username of the account that initiated the session
- **Create Branch**: `git checkout -b {username}/{description}`
- **Track Remote**: `git branch -u origin {branch-name}`

### Commit Protocol
1. **Stage Changes**: `git add <specific-files>` (prefer explicit files over `git add .`)
2. **Create Commit**: Use git commit with descriptive message ending with session URL
3. **Message Format**:
   ```
   Brief summary of changes

   https://claude.ai/code/session_ID
   ```
4. **Push Changes**: `git push -u origin {branch-name}` (uses `-u` flag for first push)

### Pull Request Workflow
1. **Push branch** to remote
2. **Create PR**: Use `gh pr create --title "..." --body "..."`
3. **Mark Ready**: `gh pr ready` when complete and tests pass
4. **Quality Gates**:
   - Code changes: Run `.\privatebuild.ps1` + `.\acceptancetests.ps1` before PR ready
   - Docs-only: Can skip builds
   - If tests fail: Fix issues and create NEW commit (don't amend)

## Database Migrations

### AliaSQL Migration System
- **Location**: `src/Database/scripts/Update/` (numbered sequence: 001, 003, 004, etc.)
- **Technology**: AliaSQL (custom migration tool)
- **Database**: SQL Server (LocalDB for development, Azure SQL for cloud)

### Adding New Migrations
1. Examine existing scripts in `src/Database/scripts/Update/` to find next number
2. Create script: `src/Database/scripts/Update/{###}_Description.sql` (e.g., `005_AddWorkOrderNotes.sql`)
3. **IMPORTANT**: Use TABS (not spaces) for indentation in SQL scripts
4. Script structure:
   - Use standard SQL syntax compatible with SQL Server
   - No transaction wrapping (AliaSQL handles it)
   - Test locally first
5. Apply locally: Run `.\privatebuild.ps1` (includes database migration step)

### Migration Directory Structure
```
src/Database/scripts/
├── Update/           # Numbered migration scripts (001, 002, 003...)
├── Rebuild/          # Initial schema creation scripts
├── TestData/         # Test data population scripts
└── AliaSQL.exe       # AliaSQL runner executable
```

### Local Development Database Setup
- **Server**: `(LocalDb)\MSSQLLocalDB` (default local SQL Server instance)
- **Database Name**: `ChurchBulletin`
- **Auto-migration**: Runs during `.\privatebuild.ps1`
- **Manual trigger**: `MigrateDatabaseLocal -databaseServerFunc "(LocalDb)\MSSQLLocalDB" -databaseNameFunc "ChurchBulletin"`

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

## File Organization Quick Reference

| Concern | Location | Key Files |
|---------|----------|-----------|
| **Domain Models** | `src/Core/Model/` | `WorkOrder.cs`, `Employee.cs`, `WorkOrderStatus.cs`, `Role.cs` |
| **Queries (CQRS)** | `src/Core/Queries/` | `EmployeeByUserNameQuery.cs`, `WorkOrderByNumberQuery.cs` |
| **State Commands** | `src/Core/Model/StateCommands/`, `src/Core/Services/` | `IStateCommand.cs` |
| **EF Core Context** | `src/DataAccess/` | `DataContext.cs` |
| **Entity Mappings** | `src/DataAccess/Mappings/` | `*Map.cs` files |
| **Query/Command Handlers** | `src/DataAccess/Handlers/` | Query/command handler implementations |
| **Bus/MediatR** | `src/Core/` | `IBus.cs` interface |
| **Health Checks** | `src/DataAccess/`, `src/LlmGateway/`, `src/UI/Server/` | `*HealthCheck.cs` |
| **Blazor Components** | `src/UI/Client/Components/`, `src/UI/Server/Components/` | `*.razor`, `*.razor.cs` |
| **API Controllers** | `src/UI/Api/Controllers/`, `src/UI/Server/Controllers/` | HTTP endpoints |
| **Worker Handlers** | `src/Worker/Handlers/`, `src/Worker/Sagas/` | Message handlers and orchestration |
| **DI Configuration** | `src/UI/Server/` | `UIServiceRegistry.cs` |
| **Acceptance Tests** | `src/AcceptanceTests/` | `*Tests.cs` (organized by feature) |
| **Unit Tests** | `src/UnitTests/` | Test fixtures for all layers |
| **Integration Tests** | `src/IntegrationTests/` | Database integration, end-to-end |
| **DB Migrations** | `src/Database/scripts/Update/` | `###_Description.sql` |

## Architecture Documentation

### Reference Documentation
- **Cursor Rules**:
  - `.cursor/rules/codebase-structure.mdc` - Solution layout, key paths, domain snapshot
  - `.cursor/rules/cloud-agent-instructions.mdc` - Agent scope, workflow, issue protocol
- **Copilot Instructions**:
  - `.github/copilot-instructions.md` - Full coding standards, testing, branch naming
  - `.github/copilot-code-review-instructions.md` - PR review checklist
- **Project Docs**: CLAUDE.md (this file), README.md, quickstart.md

### Architecture Diagrams
PlantUML diagrams in `arch/`:
- `arch-c4-system.puml` - System context diagram
- `arch-c4-container-deployment.puml` - Container and deployment view
- `arch-c4-component-project-dependencies.puml` - Project dependency diagram
- `arch-c4-class-domain-model.puml` - Domain model class diagram (WorkOrder, Employee, WorkOrderStatus, Role)

### Workflow Documentation
- `arch/WorflowForSaveDraftCommand.md` - Draft work order workflow
- `arch/WorflowForDraftToAssignedCommand.md` - Assignment workflow
- `arch/WorflowForAssignedToInProgressCommand.md` - In-progress workflow
- `arch/WorflowForInProgressToCompleteCommand.md` - Completion workflow

## AI Assistant Workflow Guide

### Starting a New Task
1. **Explore first**: Read relevant CLAUDE.md sections and cursor rules before making changes
2. **Find examples**: Look for similar patterns in the codebase
3. **Respect architecture**: Never move code across layers; expand inner layers only
4. **Plan tests**: Consider unit, integration, and acceptance tests from the start
5. **Check approvals**: Verify if changes require approval (packages, SDK, build scripts)

### Making Changes
1. **Read before edit**: Always use Read tool to examine files before editing
2. **One concern per PR**: Keep changes focused and atomic
3. **Follow patterns**: Match existing naming, structure, and coding style
4. **Update all layers**: Consider implications across Core→DataAccess→UI
5. **Test coverage**: Write tests alongside code; don't make untested changes

### Testing Before Commit
1. **Run privatebuild**: `.\privatebuild.ps1` - catches most issues
2. **Run acceptancetests**: `.\acceptancetests.ps1` - full system validation
3. **Fix immediately**: If tests fail, create new commit with fixes (don't amend)
4. **All must pass**: Never commit with failing tests

### Git & PR Workflow
1. **Create branch**: `git checkout -b {username}/{description}`
2. **Stage deliberately**: `git add <specific-files>` (not `git add .`)
3. **Commit with message**: Include descriptive message + session URL
4. **Push up-front**: `git push -u origin {branch}` early
5. **Create PR**: After tests pass, use `gh pr create`
6. **Mark ready**: Only after `.\acceptancetests.ps1` passes, run `gh pr ready`

### Common Tasks
- **Bug fix**: Write failing test → fix code → verify all tests pass
- **Feature**: Design → Core models/queries → DataAccess handlers → UI implementation → tests
- **DB change**: Create migration script → test locally → add integration tests
- **API endpoint**: Create query/command → add handler → add controller → acceptance test

## Branch Naming Convention

All branches must be created inside a folder matching the username of the account creating the branch. The format is `{username}/{branch-description}`.

- For user `jeffreypalermo`, branches go under `jeffreypalermo/` (e.g., `jeffreypalermo/fix-work-order-status`)
- For user `johnsmith`, branches go under `johnsmith/` (e.g., `johnsmith/add-employee-search`)
- For AI agents (Claude, Copilot, Cursor), use the username of the account that initiated the session

## Critical Restrictions (Read Carefully)

### DO NOT (Without Explicit Approval)
- **Add NuGet packages**: Zero tolerance - always ask before adding any package
- **Change .NET SDK version**: Currently 10.0 - do not upgrade without approval
- **Modify build scripts**: `build.ps1`, `build.bat`, `BuildFunctions.ps1` - frozen without approval
- **Modify `.octopus/` directories**: Octopus deployment configuration - off-limits
- **Change architecture layers**: Dependency rules are strict, never move code between layers
- **Use assertion libraries other than Shouldly**: FluentAssertions and Assert.That are forbidden

### Development Rules
- **Use TABS** (not spaces) in SQL migration scripts
- **Commit protocol**: Always push automatically when on a branch; add/commit/push without asking
- **Test-after approach**: Generate code first, then write tests
- **Test naming**: Use "Stub" prefix for test doubles (never "Mock", "Fake", "Test")
- **Blazor components**: Prefix test methods with "Should" or "When" (e.g., `ShouldDisplayWorkOrder()`)
- **Git safety**: Never use `git push --force` or destructive git commands without explicit user instruction
- **Session tracking**: End all commit messages with session URL for audit trail

## Important Notes

- **Onion Architecture**: Strictly maintain dependency rules
  - Core → zero project dependencies
  - DataAccess → Core only
  - UI/Worker → can reference multiple layers
- **Error handling**: Only validate at system boundaries (user input, external APIs)
- **Premature abstraction**: Don't create helpers for one-time operations; KISS principle applies
- **Unused code**: Delete completely; don't rename or add "removed" comments
- **Backwards compatibility**: Change code directly; no compatibility shims needed
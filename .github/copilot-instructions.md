# Coding Standards and Practices

This file provides standards for GitHub Copilot to follow when generating code for this project. See also `CLAUDE.md` and `.cursor/rules/codebase-structure.mdc` for solution layout and key paths.

## Quick Reference (AI Tools: Read This First)

**Stack:** .NET 10.0 | Blazor WASM + Server | EF Core 10 | SQL Server | Onion Architecture | Aspire

**Solution:** `src/ChurchBulletin.sln` — Core, DataAccess, Database, UI.Server, UI.Client, UI.Api, UI.Shared, LlmGateway, ChurchBulletin.AppHost, ChurchBulletin.ServiceDefaults, Worker, UnitTests, IntegrationTests, AcceptanceTests.

**Key Paths:**
- Domain models: `src/Core/` (WorkOrder, Employee, WorkOrderStatus, Role)
- Queries/state commands: `src/Core/Queries/`, `src/Core/Model/StateCommands/`
- Data access: `src/DataAccess/` (EF Core, MediatR handlers in `Handlers/`, `Mappings/`)
- UI Server: `src/UI/Server/` (Blazor host, DI via Lamar)
- UI Client: `src/UI/Client/` (Blazor WASM)
- Worker service: `src/Worker/` (hosted endpoint worker)
- DB migrations: `src/Database/scripts/Update/` (DbUp scripts, numbered ###_Name.sql, TABS)
- Tests: `src/UnitTests/`, `src/IntegrationTests/`, `src/AcceptanceTests/`

**Domain Model:**
- `WorkOrder`: Number, Title, Description, RoomNumber, Assignee (Employee), Status, Creator (Employee), AssignedDate, CreatedDate, CompletedDate
- `Employee`: UserName, FirstName, LastName, EmailAddress, Roles
- `WorkOrderStatus`: Draft, Assigned, InProgress, Complete, Cancelled
- `Role`: Name, CanCreateWorkOrder, CanFulfillWorkOrder

**Architecture Rules (Strict):**
- Core → no dependencies (domain models, interfaces, queries)
- DataAccess → references Core only (EF, handlers)
- UI/Worker/AppHost → outer layer (orchestration and hosting)
- NO new NuGet packages without approval
- NO .NET SDK version changes without approval

**Testing Rules:**
- Framework: NUnit 4.x + Shouldly (NOT FluentAssertions)
- Test doubles: prefix "Stub" (NOT "Mock")
- Pattern: AAA without comments
- Naming: `[Method]_[Scenario]_[Expected]`

**Code Style:**
- PascalCase classes/methods, camelCase variables
- Small focused methods, nullable reference types
- XML docs on public APIs

---

## Project Overview

This is a Work Order management application built with:
- **.NET 10.0** - Primary framework
- **Blazor** - UI framework (WebAssembly + Server)
- **Entity Framework Core 10** - Data access
- **SQL Server** - Database (LocalDB for development)
- **Onion Architecture** - Clean architecture pattern with Core, DataAccess, and UI layers
- **MediatR** - CQRS pattern for queries and commands

## Build Instructions

Build the project using PowerShell:
```powershell
# Full private build (includes compile, unit tests, and integration tests)
.\privatebuild.ps1

# CI build (includes packaging)
.\build.ps1 Invoke-CIBuild

# Individual build steps
.\build.ps1 Init      # Clean and restore
.\build.ps1 Compile   # Build solution
```

Or using .NET CLI:
```bash
dotnet restore src/ChurchBulletin.sln
dotnet build src/ChurchBulletin.sln --configuration Release
```

## Test Instructions

Run tests using PowerShell build script:
```powershell
# Run full private build (unit tests + integration tests)
.\privatebuild.ps1

# Run acceptance tests (full system test suite)
.\acceptancetests.ps1

# Individual test steps
.\build.ps1 UnitTests           # Run unit tests only
.\build.ps1 IntegrationTest     # Run integration tests (requires database)
```

Or using .NET CLI:
```bash
dotnet test src/UnitTests/UnitTests.csproj
dotnet test src/IntegrationTests/IntegrationTests.csproj
```

## Quality Gates

- **BEFORE committing changes to git**: Run `.\privatebuild.ps1` to ensure all unit and integration tests pass
- **BEFORE submitting any pull request**: Run `.\acceptancetests.ps1` to ensure full system acceptance tests pass
- If either script fails, use the output to diagnose and fix the problem before proceeding

## Pull Request Readiness (REQUIRED for Copilot SWE Agent)

**ALWAYS run `gh pr ready` when finished.** Do NOT leave PRs in draft state.

**If code files are changed** (*.cs, *.razor, *.sql, etc.), run builds first:

1. **Run `.\privatebuild.ps1`** - Must pass (unit tests + integration tests)
2. **Run `.\acceptancetests.ps1`** - Must pass (full system acceptance tests)
3. **Run `gh pr ready`** - Mark PR as ready for review

If either script fails:
- Diagnose the failure from the output
- Fix the issue
- Re-run both scripts from the beginning
- Do NOT mark PR ready until both pass

**If only documentation/config files changed** (*.md, *.yml, etc.):

1. **Run `gh pr ready`** - Mark PR as ready for review immediately

## Branch Naming Convention

All branches must be created inside a folder matching the username of the account creating the branch. The format is `{username}/{branch-description}`.

- For user `jeffreypalermo`, branches go under `jeffreypalermo/` (e.g., `jeffreypalermo/fix-work-order-status`)
- For user `johnsmith`, branches go under `johnsmith/` (e.g., `johnsmith/add-employee-search`)
- For AI agents (Copilot, Claude, Cursor), use the username of the account that initiated the session

## Special Project Rules

- **DO NOT** modify files in `.octopus/`, `.octopus_original_from_od/`, or build scripts without explicit approval
- **DO NOT** add new NuGet packages without approval
- **DO NOT** upgrade .NET SDK version without approval (currently 10.0.x)
- **ALWAYS** include unit tests for new functionality
- **ALWAYS** update XML documentation for public APIs
- Integration tests require SQL Server LocalDB

## General Coding Standards

- Use clean, readable code with proper indentation
- Follow C# naming conventions (PascalCase for classes/methods, camelCase for variables)
- Add XML documentation to public APIs
- Keep methods small and focused on a single responsibility
- Use nullable reference types appropriately

## Architecture Guidelines

- Follow Onion Architecture principles
- Keep business logic in Core project
- Data access should be isolated in DataAccess
- UI logic should be thin and focused on presentation
- Do not add Nuget packages or project references without approval.
- Keep existing versions of .NET SDK and libraries unless specifically instructed to upgrade. Don't add new libraries or Nuget packages unless explicitly instructed. Ask for approval to change .NET SDK version.

## Database Practices

- Use Entity Framework for data access
- Follow Commands and Queries and Handlers data access
- Create mapping files for all entities in `src/DataAccess/Mappings/`
- DB schema changes: DbUp scripts in `src/Database/scripts/Update/` (###_Description.sql, TABS)

## Testing Standards
- After code is generated, ask to generate a test next.
- All tests use Shouldly framework for assertions

### Testing Frameworks
- **NUnit**: Primary testing framework
- Avoid mocking libraries when possible
- When creating a test double, mock or stub in a test, use the naming of "StubClass". Don't put "Mock" in the name.

### Test Structure
- Follow AAA pattern (Arrange, Act, Assert), but don't add comments
- Use descriptive test names
- Prefix test methods with "Should" or "When"

### Test Categories
1. **Unit Tests**
   - Test a single unit in isolation
   - Fast execution, no infrastructure dependencies
   - Follow test-after approach (generate code first, then implement)

2. **Integration Tests**
   - Test component integration
   - May use actual database
   - Should run in CI/CD pipeline

3. **UI Tests**
   - Test user interface components
   - Use appropriate testing tools for Blazor components

### Test Naming Convention
- `[MethodName]_[Scenario]_[ExpectedResult]`
- Examples:
  - `GetWorkOrder_WithValidId_ReturnsWorkOrder`
  - `SaveChurchBulletin_WithMissingTitle_ThrowsValidationException`

### Acceptance Tests from Issues (IMPORTANT for Copilot SWE Agent)

When implementing a feature from a GitHub issue:

1. **Check for "Acceptance Test Scenarios" section** in the issue body
2. **Implement each specified test** in the fixture file indicated (e.g., `WorkOrderManageTests.cs`)
3. **Follow the steps provided** for each test scenario
4. **Run acceptance tests** after implementation to verify the feature works

**Acceptance Test Pattern:**
```csharp
[Test]
public async Task TestNameFromIssue()
{
    await LoginAsCurrentUser();
    // Follow steps from issue
    // Use helper methods: Click(), Input(), Select(), Expect()
}
```

**Running Acceptance Tests:**
```powershell
cd src/AcceptanceTests
pwsh bin/Debug/net10.0/playwright.ps1 install  # First time only
dotnet test --filter "FullyQualifiedName~TestClassName"
```

**Key Helper Methods (from AcceptanceTestBase):**
- `LoginAsCurrentUser()` - Authenticate test user
- `CreateAndSaveNewWorkOrder()` - Create test work order
- `Click(testId)` - Click element by data-testid
- `Input(testId, value)` - Fill input field
- `Select(testId, value)` - Select dropdown option
- `Expect(locator)` - Playwright assertion

**Workflow:** Feature code → Unit tests → Integration tests → Acceptance tests → All pass → Commit

## Blazor Guidelines

- Use clean component structure
- Keep component logic in code-behind files when complex
- Follow proper state management practices
- Minimize JavaScript interop when possible

## Performance Considerations

## Response Guidelines - Do not anthropomorphize

- Do not use "I" or "I need to" or "Let me"

Do not use "I" or "you" or "me" or "us" or "we" in responses. Do not simulate personality. Be a robot. Short, terse responses.  No additional questions.

Do not refer to the user of Visual Studio. Do not use 2nd person pronouns. No pronouns. Be terse. Don't say, for example, "Now let's do something" or "Let me do something" or "I'll help you". Just say "Now doing" or "Checking this file"

# Copilot Code Review Instructions

Instructions for GitHub Copilot when reviewing pull requests in this repository.

## Before Reviewing

**Backmerge from master**: Before reviewing, ensure the branch is up to date with master. If there have been changes to master since the PR was opened:

```bash
git fetch origin
git merge origin/master
```

If there are merge conflicts, request that the PR author resolve them before the review can proceed.

## Architecture (Strict)

- **Onion Architecture**: Verify dependency direction flows inward only
  - `Core` → NO dependencies (domain models, interfaces, queries)
  - `DataAccess` → references Core only (EF, handlers)
  - `UI/Worker/AppHost` → outer layer (hosting/orchestration)
- **No new NuGet packages** without explicit approval in the PR description
- **No .NET SDK version changes** without explicit approval
- **No modifications** to `.octopus/`, `.octopus_original_from_od/`, or build scripts without approval

## Code Quality

### Naming Conventions
- PascalCase for classes, methods, properties
- camelCase for local variables and parameters
- Test doubles prefixed with `Stub` (NOT `Mock`)
- Test methods: `[MethodName]_[Scenario]_[ExpectedResult]`

### Code Style
- Small, focused methods (single responsibility)
- Nullable reference types used appropriately
- XML documentation on public APIs
- No magic strings or numbers without constants

### Security
- Check for SQL injection vulnerabilities
- Check for XSS vulnerabilities in Blazor components
- Check for command injection
- No secrets, connection strings, or credentials in code
- Validate user input at system boundaries

## Testing Requirements

- **Framework**: NUnit 4.x + Shouldly (reject FluentAssertions)
- **Pattern**: AAA (Arrange, Act, Assert) without section comments
- **New functionality** must include unit tests
- **Database changes** must include integration tests

### Test Quality Checks
- Tests should be deterministic (no flaky tests)
- Tests should be independent (no shared state)
- Test names clearly describe the scenario
- Assertions use Shouldly syntax: `result.ShouldBe(expected)`

## Database Changes

- Migration scripts in `src/Database/scripts/Update/` use AliaSQL
- Scripts must be numbered sequentially (###_Name.sql)
- Scripts must use TABS for indentation
- Verify scripts are idempotent where possible

## Pull Request Quality

### Required for Approval
- [ ] Code compiles without warnings
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] No security vulnerabilities introduced
- [ ] Architecture rules followed
- [ ] Branch is up to date with master (backmerged)

### Automatic Rejection Reasons
- New NuGet packages without approval
- .NET SDK version changes without approval
- FluentAssertions instead of Shouldly
- Test doubles named with "Mock" prefix
- Modifications to protected directories without approval
- Secrets or credentials in code

## Domain Model Reference

When reviewing changes to domain entities, verify consistency with:
- `WorkOrder`: Number, Title, Description, RoomNumber, Assignee (Employee), Status, Creator (Employee), AssignedDate, CreatedDate, CompletedDate
- `Employee`: UserName, FirstName, LastName, EmailAddress, Roles
- `WorkOrderStatus`: Draft, Assigned, InProgress, Complete, Cancelled
- `Role`: Name, CanCreateWorkOrder, CanFulfillWorkOrder

## Key Paths Reference

- Domain models: `src/Core/`
- Data access: `src/DataAccess/`
- UI Server: `src/UI/Server/`
- UI Client: `src/UI/Client/`
- Worker service: `src/Worker/`
- DB migrations: `src/Database/scripts/Update/`
- Tests: `src/UnitTests/`, `src/IntegrationTests/`, `src/AcceptanceTests/`

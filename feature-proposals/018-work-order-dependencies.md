## Why
Some work requests must be completed before others can start (e.g., electrical work before painting, demolition before construction). Dependencies prevent premature status transitions and ensure work is performed in the correct sequence, reducing rework and safety issues.

## What Changes
- Add `WorkRequestDependency` entity to `src/Core/Model/` with properties: Id (Guid), WorkRequestId (Guid), DependsOnWorkRequestId (Guid)
- Add navigation property `Dependencies` (ICollection<WorkRequestDependency>) to `WorkRequest` domain model
- Add `AddDependencyCommand` to `src/Core/Model/StateCommands/` containing WorkRequestId and DependsOnWorkRequestId
- Add `RemoveDependencyCommand` to `src/Core/Model/StateCommands/` containing DependencyId
- Add validation in the Assigned-to-InProgress state transition (Begin command) to check that all dependency work requests are in Complete status
- Add EF Core mapping for `WorkRequestDependency` in DataAccess
- Add handlers for dependency commands in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkRequestDependency` table with FKs to `WorkRequest`
- Add a dependency section on the `WorkRequestManage` page displaying dependent work requests with their statuses and a work request number search to add new dependencies
- Add validation preventing circular dependencies

## Capabilities
### New Capabilities
- Users can add dependencies between work requests (this work request depends on another)
- Users can remove dependencies from a work request
- The system prevents transitioning to InProgress if any dependency is not Complete
- Dependency section shows the status of each dependent work request
- Circular dependency detection prevents creating dependency loops

### Modified Capabilities
- The Begin state transition (Assigned to InProgress) is modified to validate all dependencies are Complete
- WorkRequestManage page includes a new dependency section

## Impact
- **Core** — New `WorkRequestDependency` entity; new `AddDependencyCommand` and `RemoveDependencyCommand`; Begin command validation enhanced
- **DataAccess** — EF Core mapping for `WorkRequestDependency`; new handlers; modified Begin handler to check dependencies
- **UI.Shared** — Dependency section component on `WorkRequestManage` page
- **Database** — New migration script creating `WorkRequestDependency` table with two FK columns to `WorkRequest`

## Acceptance Criteria
### Unit Tests
- `AddDependencyCommand_ShouldCreateDependency` — verify dependency relationship is created
- `AddDependencyCommand_ShouldRejectCircularDependency` — verify adding a dependency that creates a cycle is rejected
- `AddDependencyCommand_ShouldRejectSelfDependency` — verify a work request cannot depend on itself
- `BeginCommand_ShouldReject_WhenDependencyNotComplete` — verify InProgress transition fails when a dependency is not Complete
- `BeginCommand_ShouldSucceed_WhenAllDependenciesComplete` — verify InProgress transition succeeds when all dependencies are Complete
- `WorkRequestManage_ShouldRenderDependencySection` — bUnit test verifying dependency section renders with existing dependencies

### Integration Tests
- `WorkRequestDependency_ShouldPersistAndRetrieve` — create a dependency and verify it round-trips through the database
- `BeginCommand_WithIncompleteDependency_ShouldThrowValidationException` — attempt to begin a work request with incomplete dependencies and verify it fails with appropriate error
- `RemoveDependencyCommand_ShouldDeleteDependency` — remove a dependency and verify it is deleted from the database

### Acceptance Tests
- Navigate to a work request, add a dependency on another work request by number, and verify the dependency appears in the dependency section with its status
- Attempt to begin a work request that has an incomplete dependency and verify an error message is displayed
- Complete the dependency work request, then successfully begin the dependent work request

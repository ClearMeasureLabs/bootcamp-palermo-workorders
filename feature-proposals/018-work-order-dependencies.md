## Why
Some work orders must be completed before others can start (e.g., electrical work before painting, demolition before construction). Dependencies prevent premature status transitions and ensure work is performed in the correct sequence, reducing rework and safety issues.

## What Changes
- Add `WorkOrderDependency` entity to `src/Core/Model/` with properties: Id (Guid), WorkOrderId (Guid), DependsOnWorkOrderId (Guid)
- Add navigation property `Dependencies` (ICollection<WorkOrderDependency>) to `WorkOrder` domain model
- Add `AddDependencyCommand` to `src/Core/Model/StateCommands/` containing WorkOrderId and DependsOnWorkOrderId
- Add `RemoveDependencyCommand` to `src/Core/Model/StateCommands/` containing DependencyId
- Add validation in the Assigned-to-InProgress state transition (Begin command) to check that all dependency work orders are in Complete status
- Add EF Core mapping for `WorkOrderDependency` in DataAccess
- Add handlers for dependency commands in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkOrderDependency` table with FKs to `WorkOrder`
- Add a dependency section on the `WorkOrderManage` page displaying dependent work orders with their statuses and a work order number search to add new dependencies
- Add validation preventing circular dependencies

## Capabilities
### New Capabilities
- Users can add dependencies between work orders (this work order depends on another)
- Users can remove dependencies from a work order
- The system prevents transitioning to InProgress if any dependency is not Complete
- Dependency section shows the status of each dependent work order
- Circular dependency detection prevents creating dependency loops

### Modified Capabilities
- The Begin state transition (Assigned to InProgress) is modified to validate all dependencies are Complete
- WorkOrderManage page includes a new dependency section

## Impact
- **Core** — New `WorkOrderDependency` entity; new `AddDependencyCommand` and `RemoveDependencyCommand`; Begin command validation enhanced
- **DataAccess** — EF Core mapping for `WorkOrderDependency`; new handlers; modified Begin handler to check dependencies
- **UI.Shared** — Dependency section component on `WorkOrderManage` page
- **Database** — New migration script creating `WorkOrderDependency` table with two FK columns to `WorkOrder`

## Acceptance Criteria
### Unit Tests
- `AddDependencyCommand_ShouldCreateDependency` — verify dependency relationship is created
- `AddDependencyCommand_ShouldRejectCircularDependency` — verify adding a dependency that creates a cycle is rejected
- `AddDependencyCommand_ShouldRejectSelfDependency` — verify a work order cannot depend on itself
- `BeginCommand_ShouldReject_WhenDependencyNotComplete` — verify InProgress transition fails when a dependency is not Complete
- `BeginCommand_ShouldSucceed_WhenAllDependenciesComplete` — verify InProgress transition succeeds when all dependencies are Complete
- `WorkOrderManage_ShouldRenderDependencySection` — bUnit test verifying dependency section renders with existing dependencies

### Integration Tests
- `WorkOrderDependency_ShouldPersistAndRetrieve` — create a dependency and verify it round-trips through the database
- `BeginCommand_WithIncompleteDependency_ShouldThrowValidationException` — attempt to begin a work order with incomplete dependencies and verify it fails with appropriate error
- `RemoveDependencyCommand_ShouldDeleteDependency` — remove a dependency and verify it is deleted from the database

### Acceptance Tests
- Navigate to a work order, add a dependency on another work order by number, and verify the dependency appears in the dependency section with its status
- Attempt to begin a work order that has an incomplete dependency and verify an error message is displayed
- Complete the dependency work order, then successfully begin the dependent work order

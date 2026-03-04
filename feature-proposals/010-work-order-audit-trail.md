## Why
Tracking who changed what and when on a work order provides accountability and aids troubleshooting. An audit trail records every state transition and field change, creating a complete history of work order modifications for compliance and dispute resolution.

## What Changes
- Add `AuditEntry` entity to `src/Core/Model/` with properties: Id (Guid), WorkOrderId (Guid), EmployeeId (Guid), Action (string), OldValue (string, nullable), NewValue (string, nullable), Timestamp (DateTime)
- Add `WorkOrderAuditQuery` to `src/Core/Queries/` to retrieve audit entries for a given work order
- Add EF Core mapping for `AuditEntry` in DataAccess
- Add handler for `WorkOrderAuditQuery` in `src/DataAccess/Handlers/`
- Modify `StateCommandHandler` in DataAccess to record an `AuditEntry` for every state transition (capturing old status, new status, and the employee who performed the action)
- Add a new DbUp migration script creating the `AuditEntry` table with foreign keys to `WorkOrder` and `Employee`
- Add an audit history section on the `WorkOrderManage` page displaying a chronological list of audit entries

## Capabilities
### New Capabilities
- Every state transition on a work order automatically creates an audit entry recording the action, old value, new value, actor, and timestamp
- Users can view the complete audit trail for a work order on its manage page
- Audit entries are immutable once created

### Modified Capabilities
- StateCommandHandler is modified to generate audit entries alongside state transitions
- WorkOrderManage page includes a new collapsible audit history section

## Impact
- **Core** — New `AuditEntry` entity; new `WorkOrderAuditQuery`
- **DataAccess** — EF Core mapping for `AuditEntry`; `StateCommandHandler` modified to write audit entries; new query handler
- **UI.Shared** — Audit history component on `WorkOrderManage` page
- **Database** — New migration script creating `AuditEntry` table with FK to `WorkOrder` and `Employee`

## Acceptance Criteria
### Unit Tests
- `AuditEntry_ShouldRecordAction` — verify audit entry captures the action string
- `AuditEntry_ShouldRecordOldAndNewValues` — verify old and new values are captured
- `StateCommandHandler_ShouldCreateAuditEntry_OnStatusChange` — verify an audit entry is created when a state command executes
- `WorkOrderManage_ShouldRenderAuditHistorySection` — bUnit test verifying audit history section renders with entries

### Integration Tests
- `AuditEntry_ShouldPersistAndRetrieve` — create an audit entry and verify it round-trips through the database
- `StateCommandHandler_ShouldPersistAuditEntry_WhenTransitioningStatus` — execute a status transition and verify the corresponding audit entry exists in the database
- `WorkOrderAuditQuery_ShouldReturnEntriesInChronologicalOrder` — perform multiple transitions and verify audit entries are returned in timestamp order

### Acceptance Tests
- Create a new work order, assign it, begin work, and complete it, then verify the audit history section shows four entries (Draft creation, Assign, Begin, Complete) with correct timestamps and actor names
- Verify each audit entry displays the old status and new status values

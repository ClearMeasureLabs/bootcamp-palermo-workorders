## Why
Tracking who changed what and when on a work request provides accountability and aids troubleshooting. An audit trail records every state transition and field change, creating a complete history of work request modifications for compliance and dispute resolution.

## What Changes
- Add `AuditEntry` entity to `src/Core/Model/` with properties: Id (Guid), WorkRequestId (Guid), EmployeeId (Guid), Action (string), OldValue (string, nullable), NewValue (string, nullable), Timestamp (DateTime)
- Add `WorkRequestAuditQuery` to `src/Core/Queries/` to retrieve audit entries for a given work request
- Add EF Core mapping for `AuditEntry` in DataAccess
- Add handler for `WorkRequestAuditQuery` in `src/DataAccess/Handlers/`
- Modify `StateCommandHandler` in DataAccess to record an `AuditEntry` for every state transition (capturing old status, new status, and the employee who performed the action)
- Add a new DbUp migration script creating the `AuditEntry` table with foreign keys to `WorkRequest` and `Employee`
- Add an audit history section on the `WorkRequestManage` page displaying a chronological list of audit entries

## Capabilities
### New Capabilities
- Every state transition on a work request automatically creates an audit entry recording the action, old value, new value, actor, and timestamp
- Users can view the complete audit trail for a work request on its manage page
- Audit entries are immutable once created

### Modified Capabilities
- StateCommandHandler is modified to generate audit entries alongside state transitions
- WorkRequestManage page includes a new collapsible audit history section

## Impact
- **Core** — New `AuditEntry` entity; new `WorkRequestAuditQuery`
- **DataAccess** — EF Core mapping for `AuditEntry`; `StateCommandHandler` modified to write audit entries; new query handler
- **UI.Shared** — Audit history component on `WorkRequestManage` page
- **Database** — New migration script creating `AuditEntry` table with FK to `WorkRequest` and `Employee`

## Acceptance Criteria
### Unit Tests
- `AuditEntry_ShouldRecordAction` — verify audit entry captures the action string
- `AuditEntry_ShouldRecordOldAndNewValues` — verify old and new values are captured
- `StateCommandHandler_ShouldCreateAuditEntry_OnStatusChange` — verify an audit entry is created when a state command executes
- `WorkRequestManage_ShouldRenderAuditHistorySection` — bUnit test verifying audit history section renders with entries

### Integration Tests
- `AuditEntry_ShouldPersistAndRetrieve` — create an audit entry and verify it round-trips through the database
- `StateCommandHandler_ShouldPersistAuditEntry_WhenTransitioningStatus` — execute a status transition and verify the corresponding audit entry exists in the database
- `WorkRequestAuditQuery_ShouldReturnEntriesInChronologicalOrder` — perform multiple transitions and verify audit entries are returned in timestamp order

### Acceptance Tests
- Create a new work request, assign it, begin work, and complete it, then verify the audit history section shows four entries (Draft creation, Assign, Begin, Complete) with correct timestamps and actor names
- Verify each audit entry displays the old status and new status values

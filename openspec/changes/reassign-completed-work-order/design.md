## Context

The ChurchBulletin system uses a state machine pattern for work order lifecycle management. State commands are `record` types extending `StateCommandBase(WorkOrder, Employee)` that define begin/end statuses, authorization rules, and execute behavior. All commands are processed by a single `StateCommandHandler` MediatR handler. The current state machine has 6 commands covering Draft, Assigned, InProgress, Complete, and Cancelled statuses, but lacks a transition from Complete back to Assigned.

The `WorkOrder.CanReassign()` method currently only returns true for Draft status, used by the UI to determine when to show the assignee picker. This needs to also cover Complete status since reassigning a completed work order implies re-assignment.

## Goals / Non-Goals

**Goals:**
- Allow the creator of a completed work order to reassign it, transitioning it from Complete to Assigned
- Follow the exact same patterns as existing state commands (DraftToAssigned, AssignedToCancelled, etc.)
- Reset `CompletedDate` to null and set `AssignedDate` to current time upon reassignment
- Register the new command in `StateCommandList` so the UI automatically surfaces the action
- Maintain full test coverage with unit and integration tests

**Non-Goals:**
- Allowing the assignee (rather than the creator) to trigger this transition
- Changing the assignee during this transition (the assignee remains the same; to change the assignee the creator uses the UI's assignee picker which is controlled by `CanReassign()`)
- Adding any new database columns or schema changes
- Modifying the existing UI components (the state machine UI pattern handles this automatically)

## Decisions

### Decision 1: Creator authorization (not assignee)

**Rationale:** The creator owns the work order lifecycle decisions. The pattern is consistent: the creator assigns (DraftToAssigned), cancels (AssignedToCancelled), and now reassigns (CompleteToAssigned). The assignee handles work execution transitions (AssignedToInProgress, InProgressToAssigned/Shelve, InProgressToComplete).

### Decision 2: Reset CompletedDate, set AssignedDate

**Rationale:** When a completed work order is reassigned, it is no longer complete. Clearing `CompletedDate` reflects the accurate state. Setting `AssignedDate` to the current time follows the same pattern as `DraftToAssignedCommand`. The assignee is preserved from the previous assignment.

### Decision 3: Use verb "Reassign" / "Reassigned"

**Rationale:** "Reassign" clearly communicates the intent — the work order is being sent back for additional work. This is distinct from "Assign" (DraftToAssigned) and "Shelve" (InProgressToAssigned).

## Risks / Trade-offs

- **[Minimal risk]** This is a straightforward state command following well-established patterns in the codebase. No architectural changes required.
- **[CanReassign scope]** Updating `CanReassign()` to include Complete status means the assignee picker will be shown on completed work orders in the UI. This is desirable since reassignment may involve changing the assignee.

## Open Questions

- None. The implementation follows existing patterns directly.

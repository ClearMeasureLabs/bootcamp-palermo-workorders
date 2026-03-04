## Why

A work order that has been completed may need to be reworked. Currently, once a work order reaches the Complete status, there is no way to transition it back to an active state. The creator of the work order should be able to reassign a completed work order, moving it back to Assigned status so that work can continue. This mirrors the existing pattern where the creator controls assignment (DraftToAssigned) and cancellation (AssignedToCancelled).

## What Changes

- New state command `CompleteToAssignedCommand` that transitions a work order from Complete to Assigned
- The creator of the work order is the authorized user for this transition
- The command resets `CompletedDate` to null and sets `AssignedDate` to the current date/time
- `WorkOrder.CanReassign()` updated to also return true when the work order is in Complete status
- The new command is registered in `StateCommandList`

## Capabilities

### New Capabilities
- `complete-to-assigned-command`: State command allowing the creator to reassign a completed work order back to Assigned status

### Modified Capabilities
- `StateCommandList`: Updated to include the new `CompleteToAssignedCommand` (7 commands total)
- `WorkOrder.CanReassign()`: Updated to return true for Complete status in addition to Draft status

## Impact

- **Core model**: New `CompleteToAssignedCommand` record in `StateCommands` namespace
- **State machine**: One new transition edge (Complete → Assigned), increasing command count from 6 to 7
- **Database**: No schema changes — reuses existing `AssignedDate`, `CompletedDate`, and `Status` columns
- **UI**: The Blazor UI will automatically surface the "Reassign" action button for completed work orders when the current user is the creator, via the existing `StateCommandList.GetValidStateCommands()` mechanism
- **Tests**: New unit tests and integration tests following existing patterns

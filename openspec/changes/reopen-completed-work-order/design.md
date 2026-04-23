## Context

The ChurchBulletin work order system models a linear lifecycle: Draft -> Assigned -> InProgress -> Complete (with a Cancelled branch from Assigned). Each transition is implemented as a `StateCommandBase` record in `src/Core/Model/StateCommands/` that declares its begin/end status, authorization rule, and optional side effects (e.g., setting `CompletedDate`). The `StateCommandList` class registers all commands, and the `StateCommandHandler` (MediatR handler) persists the transition via EF Core.

Currently there is no backward transition from Complete. The `InProgressToAssignedCommand` ("Shelve") establishes a precedent for backward transitions, moving a work order from InProgress back to Assigned. The reopen command follows the same pattern but operates on the Complete -> InProgress edge.

## Goals / Non-Goals

**Goals:**
- Allow the assignee to reopen a completed work order, transitioning it from Complete back to InProgress
- Clear the `CompletedDate` when reopening, since the work order is no longer complete
- Follow the established `StateCommandBase` pattern exactly
- Expose the reopen action in the work order manage UI with a "Reopen" button
- Cover the new transition with unit tests, integration tests, and acceptance tests

**Non-Goals:**
- Allowing users other than the assignee to reopen (the creator cannot reopen)
- Adding a reopen reason or comment field (can be added later)
- Changing the existing state machine for any other transitions
- Adding an audit trail beyond what the existing `StateCommandHandler` logging provides

## Decisions

### Decision 1: Name the command `CompleteToInProgressCommand` with verb "Reopen"

**Rationale:** The class name follows the established `[BeginStatus]To[EndStatus]Command` naming convention (e.g., `InProgressToCompleteCommand`, `DraftToAssignedCommand`). The verb "Reopen" is user-facing and clearly communicates the intent. Past tense is "Reopened".

**Alternatives considered:**
- `ReopenCommand`: Breaks the naming convention used by all other state commands
- Using verb "Resume": Less clear — "resume" implies pausing, not completing and coming back

### Decision 2: Only the assignee can reopen

**Rationale:** The assignee is the person who performed the work and marked it complete. They are in the best position to determine whether the work needs to be revisited. This mirrors `InProgressToCompleteCommand` and `AssignedToInProgressCommand`, which are also assignee-only.

**Alternatives considered:**
- Allow the creator to reopen: The creator assigned the work but may not have visibility into completion status. Can be added later if needed.

### Decision 3: Clear `CompletedDate` on reopen

**Rationale:** The `CompletedDate` was set by `InProgressToCompleteCommand.Execute()` when the work order was completed. Reopening means the work is no longer complete, so the date should be cleared (set to `null`). When the work order is completed again, a new `CompletedDate` will be set.

## Risks / Trade-offs

- **[State machine complexity]** Adding a backward edge from Complete increases the state machine complexity slightly. -> Mitigation: The pattern is already established by `InProgressToAssignedCommand` (Shelve). The `StateCommandBase` infrastructure handles validation automatically.
- **[CompletedDate history]** Clearing `CompletedDate` loses the original completion timestamp. -> Mitigation: The `StateCommandHandler` logs all transitions. A full audit trail feature could be added separately if needed.
- **[Repeated completions]** A work order could be completed, reopened, and completed again multiple times. -> Mitigation: This is valid behavior — each completion sets a new `CompletedDate`. No artificial limit is needed.

## Open Questions

- Should there be a limit on how many times a work order can be reopened?
- Should reopening a work order trigger a notification to the creator?

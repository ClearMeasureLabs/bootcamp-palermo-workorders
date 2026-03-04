## Context

The ChurchBulletin work order system follows a state machine pattern where each transition is implemented as a `StateCommandBase` record. The current state transitions from Assigned are: `AssignedToInProgressCommand` (executed by the assignee) and `AssignedToCancelledCommand` (executed by the creator). There is no reverse transition from Assigned back to Draft, forcing creators who assign prematurely to cancel and recreate the work order.

The `AssignedToCancelledCommand` already demonstrates the pattern for a creator-initiated transition from Assigned that clears the assignee and assigned date. The new `AssignedToDraftCommand` follows the same pattern but transitions to Draft instead of Cancelled.

## Goals / Non-Goals

**Goals:**
- Allow the creator to revert a work order from Assigned back to Draft
- Clear the assignee and assigned date on reversion
- Follow the existing `StateCommandBase` pattern with no architectural changes
- Maintain full test coverage (unit and integration) consistent with existing commands

**Non-Goals:**
- Adding audit trail or history for the unassign action (beyond what `ChangeStatus` already records)
- Allowing anyone other than the creator to unassign
- Adding unassign capability from InProgress or Complete states
- UI redesign — the existing command button rendering logic handles new commands automatically

## Decisions

### Decision 1: Model as `AssignedToDraftCommand` following the `StateCommandBase` pattern

**Rationale:** Every state transition in the system is a record inheriting from `StateCommandBase`. This is the established pattern. The new command is structurally identical to `AssignedToCancelledCommand` — same begin status (Assigned), same authorization (creator only), same side effects (clear assignee and assigned date) — but with Draft as the end status instead of Cancelled.

**Alternatives considered:**
- Reusing `AssignedToCancelledCommand` with a flag: Violates single-responsibility. Cancel and unassign are semantically different operations with different end states.
- Adding an "undo" mechanism to the state machine: Over-engineered for a single reverse transition.

### Decision 2: Use "Unassign" as the transition verb

**Rationale:** "Unassign" clearly communicates the action to both the UI and MCP tool consumers. It distinguishes from "Cancel" (which moves to a terminal Cancelled state) and from "Shelve" (which is InProgress to Assigned).

**Alternatives considered:**
- "Revert": Too generic — could imply reverting content changes, not just status.
- "Reset": Implies clearing more data than just the assignment.

### Decision 3: Register in `StateCommandList` after `AssignedToCancelledCommand`

**Rationale:** The command list order affects which commands appear in the UI. Placing the new command after `AssignedToCancelledCommand` groups the two creator-initiated Assigned transitions together. The `GetValidStateCommands` method filters by `IsValid()`, so only applicable commands are shown.

## Risks / Trade-offs

- **[Minimal risk]** Adding a reverse transition could allow creators to disrupt work if an assignee is about to begin. → Mitigation: The command only works while the work order is still in Assigned status. Once the assignee moves it to InProgress, unassign is no longer valid.
- **[Low complexity]** The change touches only the state command layer with no schema, migration, or API changes. Risk of regressions is low given the isolated nature of the addition.

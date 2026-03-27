## Why

Once a work order reaches the Complete status, there is currently no way to reopen it. In practice, work that was marked complete may need to be revisited — the assignee may discover remaining tasks, a defect may surface, or the completion may have been premature. Without a reopen capability, users must create a new work order and lose the history and context of the original. Allowing the assignee to reopen a completed work order back to In Progress preserves continuity and matches real-world maintenance workflows.

## What Changes

- New state transition command (`CompleteToInProgressCommand`) that moves a work order from `Complete` back to `InProgress`
- The assignee of the work order is the only user authorized to execute this command
- The `CompletedDate` field is cleared when the work order is reopened
- UI updates to display a "Reopen" button on completed work orders when viewed by the assignee
- MCP server updated to recognize the new command via the existing `execute-work-order-command` tool

## Capabilities

### New Capabilities
- `reopen-work-order`: State transition from Complete to InProgress, authorized for the assignee only

### Modified Capabilities
- The work order manage UI page gains a "Reopen" button visible to the assignee on completed work orders
- The MCP `execute-work-order-command` tool automatically supports the new command via the existing `StateCommandList` registration

## Impact

- **Core project**: New `CompleteToInProgressCommand` record in `src/Core/Model/StateCommands/`, registered in `StateCommandList`
- **UI project**: "Reopen" button added to the work order manage page, visible when the work order is Complete and the current user is the assignee
- **Database**: No schema changes — reuses existing `Status` and `CompletedDate` columns
- **Tests**: New unit tests for the command, integration tests for the handler, and acceptance tests for the UI button
- **MCP Server**: No code changes required — the `execute-work-order-command` tool resolves commands dynamically from `StateCommandList`

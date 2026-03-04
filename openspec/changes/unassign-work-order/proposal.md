## Why

When a creator assigns a work order too quickly or to the wrong person, there is no way to revert it back to Draft status. The only option from Assigned is to cancel the work order entirely and recreate it, which loses the original work order number and history. A lightweight "unassign" transition lets the creator correct assignment mistakes without destroying the work order.

## What Changes

- Add a new state command `AssignedToDraftCommand` that transitions a work order from Assigned back to Draft
- Only the original creator of the work order can execute this command
- The assignee and assigned date are cleared when the work order returns to Draft
- The state transition map and UI are updated to expose this action to the creator
- The MCP server's `execute-work-order-command` tool and reference resources automatically support the new command through the existing `StateCommandList` pattern

## Capabilities

### New Capabilities
- `assigned-to-draft-command`: State command allowing the creator to unassign an assigned work order, reverting it to Draft status with the assignee cleared

### Modified Capabilities

## Impact

- **Core**: New `AssignedToDraftCommand` record in `src/Core/Model/StateCommands/`, registered in `StateCommandList`
- **Architecture diagram**: `arch/arch-state-workorder.md` updated with the new `Assigned --> Draft` transition
- **Unit tests**: New test class following the existing 4-test pattern in `src/UnitTests/Core/Model/StateCommands/`
- **Integration tests**: New persistence test class in `src/IntegrationTests/DataAccess/Handlers/`
- **StateCommandList**: One additional command registration — existing `GetValidStateCommands`, `GetAllStateCommands`, and `GetMatchingCommand` methods work without modification
- **MCP server**: No changes needed — the `execute-work-order-command` tool dynamically resolves commands from `StateCommandList`
- **UI**: The existing command button rendering logic in the Blazor UI will automatically show the new transition when valid

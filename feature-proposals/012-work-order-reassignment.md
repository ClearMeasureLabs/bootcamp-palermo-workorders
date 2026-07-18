## Why
Currently work orders can only be assigned during the Draft-to-Assigned transition. A dedicated reassignment capability lets managers redirect work to a different team member without cancelling and recreating work orders, preserving the work order's history and continuity.

## What Changes
- Add `ReassignWorkOrderCommand` to `src/Core/Model/StateCommands/` containing WorkOrderId, NewAssigneeId, and RequestedById
- The command supports reassignment from Assigned-to-Assigned and InProgress-to-Assigned transitions with a new assignee
- Add validation that the requesting user is the work order creator (only creators can reassign)
- Add validation that the new assignee has the CanFulfillWorkOrder role
- When reassigning from InProgress, reset status to Assigned
- Add `ReassignWorkOrderCommandHandler` in `src/DataAccess/Handlers/`
- Update `WorkOrderManage` page to show a "Reassign" button and assignee selection dropdown when the work order is in Assigned or InProgress status

## Capabilities
### New Capabilities
- Creators can reassign an Assigned work order to a different employee
- Creators can reassign an InProgress work order to a different employee (status reverts to Assigned)
- Reassignment validates that the new assignee has the CanFulfillWorkOrder role

### Modified Capabilities
- WorkOrderManage page includes a "Reassign" button with assignee dropdown for Assigned and InProgress work orders
- The `CanReassign()` method on WorkOrder is utilized to control button visibility

## Impact
- **Core** — New `ReassignWorkOrderCommand` state command with creator and role validation
- **DataAccess** — New `ReassignWorkOrderCommandHandler` that updates assignee and potentially resets status
- **UI.Shared** — `WorkOrderManage` page updated with reassign button and employee dropdown

## Acceptance Criteria
### Unit Tests
- `ReassignWorkOrderCommand_ShouldChangeAssignee` — verify the assignee is updated to the new employee
- `ReassignWorkOrderCommand_ShouldResetStatusToAssigned_WhenInProgress` — verify InProgress work orders revert to Assigned on reassignment
- `ReassignWorkOrderCommand_ShouldMaintainAssignedStatus_WhenAlreadyAssigned` — verify Assigned work orders remain Assigned
- `ReassignWorkOrderCommand_ShouldRejectNonCreatorRequester` — verify only the creator can reassign
- `ReassignWorkOrderCommand_ShouldRejectAssigneeWithoutFulfillRole` — verify the new assignee must have CanFulfillWorkOrder
- `WorkOrderManage_ShouldRenderReassignButton_WhenAssigned` — bUnit test verifying button appears for Assigned work orders
- `WorkOrderManage_ShouldHideReassignButton_WhenDraft` — bUnit test verifying button is hidden for Draft work orders

### Integration Tests
- `ReassignWorkOrderCommand_ShouldPersistNewAssignee` — reassign a work order and verify the new assignee is persisted
- `ReassignWorkOrderCommand_FromInProgress_ShouldPersistAssignedStatus` — reassign an InProgress work order and verify status is Assigned in the database

### Acceptance Tests
- Navigate to an Assigned work order as the creator, click "Reassign", select a different employee, confirm, and verify the assignee is updated
- Navigate to an InProgress work order as the creator, reassign to a different employee, and verify the status changes to Assigned with the new assignee

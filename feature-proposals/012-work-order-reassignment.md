## Why
Currently work requests can only be assigned during the Draft-to-Assigned transition. A dedicated reassignment capability lets managers redirect work to a different team member without cancelling and recreating work requests, preserving the work request's history and continuity.

## What Changes
- Add `ReassignWorkRequestCommand` to `src/Core/Model/StateCommands/` containing WorkRequestId, NewAssigneeId, and RequestedById
- The command supports reassignment from Assigned-to-Assigned and InProgress-to-Assigned transitions with a new assignee
- Add validation that the requesting user is the work request creator (only creators can reassign)
- Add validation that the new assignee has the CanFulfillWorkRequest role
- When reassigning from InProgress, reset status to Assigned
- Add `ReassignWorkRequestCommandHandler` in `src/DataAccess/Handlers/`
- Update `WorkRequestManage` page to show a "Reassign" button and assignee selection dropdown when the work request is in Assigned or InProgress status

## Capabilities
### New Capabilities
- Creators can reassign an Assigned work request to a different employee
- Creators can reassign an InProgress work request to a different employee (status reverts to Assigned)
- Reassignment validates that the new assignee has the CanFulfillWorkRequest role

### Modified Capabilities
- WorkRequestManage page includes a "Reassign" button with assignee dropdown for Assigned and InProgress work requests
- The `CanReassign()` method on WorkRequest is utilized to control button visibility

## Impact
- **Core** — New `ReassignWorkRequestCommand` state command with creator and role validation
- **DataAccess** — New `ReassignWorkRequestCommandHandler` that updates assignee and potentially resets status
- **UI.Shared** — `WorkRequestManage` page updated with reassign button and employee dropdown

## Acceptance Criteria
### Unit Tests
- `ReassignWorkRequestCommand_ShouldChangeAssignee` — verify the assignee is updated to the new employee
- `ReassignWorkRequestCommand_ShouldResetStatusToAssigned_WhenInProgress` — verify InProgress work requests revert to Assigned on reassignment
- `ReassignWorkRequestCommand_ShouldMaintainAssignedStatus_WhenAlreadyAssigned` — verify Assigned work requests remain Assigned
- `ReassignWorkRequestCommand_ShouldRejectNonCreatorRequester` — verify only the creator can reassign
- `ReassignWorkRequestCommand_ShouldRejectAssigneeWithoutFulfillRole` — verify the new assignee must have CanFulfillWorkRequest
- `WorkRequestManage_ShouldRenderReassignButton_WhenAssigned` — bUnit test verifying button appears for Assigned work requests
- `WorkRequestManage_ShouldHideReassignButton_WhenDraft` — bUnit test verifying button is hidden for Draft work requests

### Integration Tests
- `ReassignWorkRequestCommand_ShouldPersistNewAssignee` — reassign a work request and verify the new assignee is persisted
- `ReassignWorkRequestCommand_FromInProgress_ShouldPersistAssignedStatus` — reassign an InProgress work request and verify status is Assigned in the database

### Acceptance Tests
- Navigate to an Assigned work request as the creator, click "Reassign", select a different employee, confirm, and verify the assignee is updated
- Navigate to an InProgress work request as the creator, reassign to a different employee, and verify the status changes to Assigned with the new assignee

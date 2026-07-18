## Why
Recurring maintenance tasks often share the same title, description, and room. Cloning an existing work request saves data entry time and reduces errors by copying known-good values into a new draft work request.

## What Changes
- Add `CloneWorkRequestCommand` to `src/Core/Model/StateCommands/` containing the source WorkRequest Id and the creator Employee Id
- Add `CloneWorkRequestCommandHandler` in `src/DataAccess/Handlers/` that loads the source work request, creates a new work request copying Title, Description, and RoomNumber, sets status to Draft, generates a new work request number, sets CreatedDate to now, and clears Assignee/AssignedDate/CompletedDate
- Add a "Clone" button on the `WorkRequestManage` page (visible for any existing work request regardless of status)
- After cloning, navigate to the newly created work request's manage page

## Capabilities
### New Capabilities
- Users can clone any existing work request into a new Draft work request
- The cloned work request copies Title, Description, and RoomNumber from the source
- The cloned work request receives a new unique number and starts in Draft status

### Modified Capabilities
- WorkRequestManage page includes a new "Clone" button in the action bar

## Impact
- **Core** — New `CloneWorkRequestCommand` containing source work request Id and creator Id
- **DataAccess** — New `CloneWorkRequestCommandHandler` that reads source and creates new work request
- **UI.Shared** — `WorkRequestManage` page updated with "Clone" button and post-clone navigation

## Acceptance Criteria
### Unit Tests
- `CloneWorkRequestCommand_ShouldCopyTitleDescriptionRoom` — verify cloned work request has same Title, Description, and RoomNumber
- `CloneWorkRequestCommand_ShouldSetStatusToDraft` — verify cloned work request status is Draft
- `CloneWorkRequestCommand_ShouldGenerateNewNumber` — verify cloned work request has a different number than the source
- `CloneWorkRequestCommand_ShouldClearAssigneeAndDates` — verify Assignee, AssignedDate, and CompletedDate are null on the clone
- `WorkRequestManage_ShouldRenderCloneButton` — bUnit test verifying "Clone" button appears for existing work requests

### Integration Tests
- `CloneWorkRequestCommand_ShouldPersistClonedWorkRequest` — clone a work request and verify the new work request exists in the database with correct field values
- `CloneWorkRequestCommand_ShouldNotModifySourceWorkRequest` — verify the original work request is unchanged after cloning

### Acceptance Tests
- Navigate to an existing Assigned work request, click "Clone", and verify a new Draft work request is created with the same title, description, and room number
- Verify the cloned work request's manage page shows Draft status and no assignee

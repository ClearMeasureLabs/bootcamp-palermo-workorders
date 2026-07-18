## Why
Recurring maintenance tasks often share the same title, description, and room. Cloning an existing work order saves data entry time and reduces errors by copying known-good values into a new draft work order.

## What Changes
- Add `CloneWorkOrderCommand` to `src/Core/Model/StateCommands/` containing the source WorkOrder Id and the creator Employee Id
- Add `CloneWorkOrderCommandHandler` in `src/DataAccess/Handlers/` that loads the source work order, creates a new work order copying Title, Description, and RoomNumber, sets status to Draft, generates a new work order number, sets CreatedDate to now, and clears Assignee/AssignedDate/CompletedDate
- Add a "Clone" button on the `WorkOrderManage` page (visible for any existing work order regardless of status)
- After cloning, navigate to the newly created work order's manage page

## Capabilities
### New Capabilities
- Users can clone any existing work order into a new Draft work order
- The cloned work order copies Title, Description, and RoomNumber from the source
- The cloned work order receives a new unique number and starts in Draft status

### Modified Capabilities
- WorkOrderManage page includes a new "Clone" button in the action bar

## Impact
- **Core** — New `CloneWorkOrderCommand` containing source work order Id and creator Id
- **DataAccess** — New `CloneWorkOrderCommandHandler` that reads source and creates new work order
- **UI.Shared** — `WorkOrderManage` page updated with "Clone" button and post-clone navigation

## Acceptance Criteria
### Unit Tests
- `CloneWorkOrderCommand_ShouldCopyTitleDescriptionRoom` — verify cloned work order has same Title, Description, and RoomNumber
- `CloneWorkOrderCommand_ShouldSetStatusToDraft` — verify cloned work order status is Draft
- `CloneWorkOrderCommand_ShouldGenerateNewNumber` — verify cloned work order has a different number than the source
- `CloneWorkOrderCommand_ShouldClearAssigneeAndDates` — verify Assignee, AssignedDate, and CompletedDate are null on the clone
- `WorkOrderManage_ShouldRenderCloneButton` — bUnit test verifying "Clone" button appears for existing work orders

### Integration Tests
- `CloneWorkOrderCommand_ShouldPersistClonedWorkOrder` — clone a work order and verify the new work order exists in the database with correct field values
- `CloneWorkOrderCommand_ShouldNotModifySourceWorkOrder` — verify the original work order is unchanged after cloning

### Acceptance Tests
- Navigate to an existing Assigned work order, click "Clone", and verify a new Draft work order is created with the same title, description, and room number
- Verify the cloned work order's manage page shows Draft status and no assignee

## Why
Updating work orders one at a time is inefficient when multiple need the same status change. Bulk operations save time for supervisors managing many work orders, especially when closing out completed tasks or cancelling obsolete requests at the end of a reporting period.

## What Changes
- Add `BulkStatusUpdateCommand` to `src/Core/Model/StateCommands/` containing a list of WorkOrder Ids and the target status
- Add validation logic ensuring all selected work orders can legally transition to the target status
- Add `BulkStatusUpdateCommandHandler` in `src/DataAccess/Handlers/` that processes each work order in a single transaction
- Add checkbox column to `WorkOrderSearch` results table for selecting multiple work orders
- Add a bulk action toolbar above the search results with a status dropdown and "Update Selected" button
- Add error display for work orders that failed validation during bulk update
- Return a result summary showing how many succeeded and which ones failed with reasons

## Capabilities
### New Capabilities
- Users can select multiple work orders on the search page using checkboxes
- Users can apply a status transition to all selected work orders in a single action
- The system validates each transition individually and reports successes and failures
- A "Select All" checkbox is available in the header row

### Modified Capabilities
- WorkOrderSearch results table gains a checkbox column and bulk action toolbar

## Impact
- **Core** — New `BulkStatusUpdateCommand` with list of work order Ids and target status; new result type for bulk operation outcomes
- **DataAccess** — New `BulkStatusUpdateCommandHandler` that loads and transitions each work order within a transaction
- **UI.Shared** — `WorkOrderSearch` page updated with checkboxes, select-all, bulk toolbar, and result summary display

## Acceptance Criteria
### Unit Tests
- `BulkStatusUpdateCommand_ShouldRequireAtLeastOneWorkOrder` — verify command rejects empty work order list
- `BulkStatusUpdateCommand_ShouldValidateStatusTransitions` — verify each work order is individually validated
- `WorkOrderSearch_ShouldRenderCheckboxColumn` — bUnit test verifying checkboxes appear in the results table
- `WorkOrderSearch_ShouldEnableBulkToolbar_WhenItemsSelected` — bUnit test verifying toolbar becomes active when checkboxes are checked

### Integration Tests
- `BulkStatusUpdateCommand_ShouldTransitionAllValidWorkOrders` — create multiple Assigned work orders, bulk cancel them, and verify all are Cancelled
- `BulkStatusUpdateCommand_ShouldReportFailures_ForInvalidTransitions` — attempt to bulk complete Draft work orders and verify appropriate error responses
- `BulkStatusUpdateCommand_ShouldBeTransactional` — verify that partial failures do not leave the database in an inconsistent state

### Acceptance Tests
- Navigate to work order search, select three Assigned work orders using checkboxes, choose "Cancel" from the bulk action dropdown, click "Update Selected", and verify all three show Cancelled status
- Attempt a bulk transition that includes an invalid work order and verify the error summary correctly identifies which work order failed and why

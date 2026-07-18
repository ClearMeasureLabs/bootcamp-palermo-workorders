## Why
Updating work requests one at a time is inefficient when multiple need the same status change. Bulk operations save time for supervisors managing many work requests, especially when closing out completed tasks or cancelling obsolete requests at the end of a reporting period.

## What Changes
- Add `BulkStatusUpdateCommand` to `src/Core/Model/StateCommands/` containing a list of WorkRequest Ids and the target status
- Add validation logic ensuring all selected work requests can legally transition to the target status
- Add `BulkStatusUpdateCommandHandler` in `src/DataAccess/Handlers/` that processes each work request in a single transaction
- Add checkbox column to `WorkRequestSearch` results table for selecting multiple work requests
- Add a bulk action toolbar above the search results with a status dropdown and "Update Selected" button
- Add error display for work requests that failed validation during bulk update
- Return a result summary showing how many succeeded and which ones failed with reasons

## Capabilities
### New Capabilities
- Users can select multiple work requests on the search page using checkboxes
- Users can apply a status transition to all selected work requests in a single action
- The system validates each transition individually and reports successes and failures
- A "Select All" checkbox is available in the header row

### Modified Capabilities
- WorkRequestSearch results table gains a checkbox column and bulk action toolbar

## Impact
- **Core** — New `BulkStatusUpdateCommand` with list of work request Ids and target status; new result type for bulk operation outcomes
- **DataAccess** — New `BulkStatusUpdateCommandHandler` that loads and transitions each work request within a transaction
- **UI.Shared** — `WorkRequestSearch` page updated with checkboxes, select-all, bulk toolbar, and result summary display

## Acceptance Criteria
### Unit Tests
- `BulkStatusUpdateCommand_ShouldRequireAtLeastOneWorkRequest` — verify command rejects empty work request list
- `BulkStatusUpdateCommand_ShouldValidateStatusTransitions` — verify each work request is individually validated
- `WorkRequestSearch_ShouldRenderCheckboxColumn` — bUnit test verifying checkboxes appear in the results table
- `WorkRequestSearch_ShouldEnableBulkToolbar_WhenItemsSelected` — bUnit test verifying toolbar becomes active when checkboxes are checked

### Integration Tests
- `BulkStatusUpdateCommand_ShouldTransitionAllValidWorkRequests` — create multiple Assigned work requests, bulk cancel them, and verify all are Cancelled
- `BulkStatusUpdateCommand_ShouldReportFailures_ForInvalidTransitions` — attempt to bulk complete Draft work requests and verify appropriate error responses
- `BulkStatusUpdateCommand_ShouldBeTransactional` — verify that partial failures do not leave the database in an inconsistent state

### Acceptance Tests
- Navigate to work request search, select three Assigned work requests using checkboxes, choose "Cancel" from the bulk action dropdown, click "Update Selected", and verify all three show Cancelled status
- Attempt a bulk transition that includes an invalid work request and verify the error summary correctly identifies which work request failed and why

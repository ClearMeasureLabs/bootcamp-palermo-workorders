## Why
Work requests that miss their due dates create operational risk and customer dissatisfaction. A dedicated overdue report enables managers to quickly identify and prioritize stalled work, reducing the number of items that slip through the cracks.

## What Changes
- Add `DueDate` property (nullable `DateOnly`) to the `WorkRequest` domain model in `src/Core/Model/`
- Add database migration script in `src/Database/scripts/Update/` to add the `DueDate` column to the `WorkRequest` table
- Update `DataContext` entity configuration in `src/DataAccess/` to map the new column
- Add `OverdueWorkRequestsQuery` to `src/Core/Queries/` returning work requests where `DueDate < today` and status is not Complete or Cancelled
- Add `OverdueWorkRequestsHandler` in `src/DataAccess/Handlers/`
- Add `OverdueWorkRequests.razor` page in `src/UI/Client/` with a sortable table showing overdue items
- Add API endpoint in `src/UI/Api/`
- Add navigation link to the report in the NavMenu

## Capabilities
### New Capabilities
- `DueDate` field on work requests, settable during creation and editing
- Dedicated overdue work requests report page listing all non-complete work requests past their due date
- Sortable columns: Work Request Number, Title, Room, Assignee, Due Date, Days Overdue
- Visual indicators (color coding) for severity based on days overdue

### Modified Capabilities
- Work request create and edit forms updated to include a `DueDate` date picker

## Impact
- `src/Core/` — modified `WorkRequest` model, new query class
- `src/DataAccess/` — updated entity configuration, new handler
- `src/Database/` — new migration script adding `DueDate` column
- `src/UI/Client/` — new report page, modified work request form components
- `src/UI/Api/` — new API endpoint
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `OverdueWorkRequestsHandler` returns only work requests with `DueDate` before today that are not Complete or Cancelled
- `OverdueWorkRequestsHandler` excludes work requests with no `DueDate` set
- `OverdueWorkRequestsHandler` excludes completed and cancelled work requests even if past due
- Days overdue calculation is accurate

### Integration Tests
- `OverdueWorkRequestsHandler` returns correct results from a seeded database with a mix of overdue, on-time, and completed work requests
- `DueDate` column persists correctly through EF Core

### Acceptance Tests
- Navigate to the overdue report page and verify overdue work requests are listed
- Create a work request with a past due date and verify it appears on the report
- Complete an overdue work request and verify it is removed from the report on refresh
- Verify the days overdue column shows the correct value

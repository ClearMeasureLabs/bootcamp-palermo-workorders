## Why
Without due dates, teams cannot track deadlines or identify overdue work requests. A DueDate field enables deadline management, helps prioritize daily work, and supports future reporting on on-time completion rates.

## What Changes
- Add `DueDate` nullable `DateTime` property to the `WorkRequest` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `DueDate` as a `datetime2` column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a nullable `DueDate` column to the `WorkRequest` table
- Update `WorkRequestManage` Blazor form to include a date picker for DueDate
- Update `WorkRequestSearch` results to display the due date column
- Add a visual indicator (CSS class) for overdue work requests (DueDate in the past and status not Complete/Cancelled)
- Update `WorkRequestSearchQuery` to support sorting by due date

## Capabilities
### New Capabilities
- Users can set an optional due date when creating or editing a work request
- Search results display due dates and visually highlight overdue work requests
- Work requests can be sorted by due date on the search page

### Modified Capabilities
- WorkRequestManage form includes a new DueDate date picker field
- WorkRequestSearch results table includes a DueDate column with overdue styling

## Impact
- **Core** — `WorkRequest` model gains nullable `DueDate` property
- **DataAccess** — EF Core mapping update for `DueDate` column; search handler updated for due date sorting
- **UI.Shared** — `WorkRequestManage` form updated with date picker; `WorkRequestSearch` page updated with column and overdue CSS indicator
- **Database** — New migration script adding nullable `DueDate` column to `WorkRequest` table

## Acceptance Criteria
### Unit Tests
- `WorkRequest_DueDate_ShouldDefaultToNull` — verify new work requests have no due date by default
- `WorkRequest_IsOverdue_WhenDueDatePastAndStatusNotComplete` — verify overdue detection logic
- `WorkRequest_IsNotOverdue_WhenDueDatePastAndStatusComplete` — verify completed work requests are not flagged overdue
- `WorkRequestManage_ShouldRenderDueDatePicker` — bUnit test verifying date picker appears on the form

### Integration Tests
- `WorkRequest_WithDueDate_ShouldPersistAndRetrieve` — save a work request with a due date and verify it round-trips through the database
- `WorkRequestSearchQuery_SortByDueDate_ShouldReturnOrderedResults` — verify search results sort correctly by due date

### Acceptance Tests
- Navigate to create work request form, set a due date, save, and verify the due date is displayed on the work request detail page
- Create a work request with a past due date, navigate to search, and verify the overdue visual indicator is present

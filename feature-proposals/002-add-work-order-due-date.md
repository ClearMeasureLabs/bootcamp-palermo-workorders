## Why
Without due dates, teams cannot track deadlines or identify overdue work orders. A DueDate field enables deadline management, helps prioritize daily work, and supports future reporting on on-time completion rates.

## What Changes
- Add `DueDate` nullable `DateTime` property to the `WorkOrder` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `DueDate` as a `datetime2` column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a nullable `DueDate` column to the `WorkOrder` table
- Update `WorkOrderManage` Blazor form to include a date picker for DueDate
- Update `WorkOrderSearch` results to display the due date column
- Add a visual indicator (CSS class) for overdue work orders (DueDate in the past and status not Complete/Cancelled)
- Update `WorkOrderSearchQuery` to support sorting by due date

## Capabilities
### New Capabilities
- Users can set an optional due date when creating or editing a work order
- Search results display due dates and visually highlight overdue work orders
- Work orders can be sorted by due date on the search page

### Modified Capabilities
- WorkOrderManage form includes a new DueDate date picker field
- WorkOrderSearch results table includes a DueDate column with overdue styling

## Impact
- **Core** — `WorkOrder` model gains nullable `DueDate` property
- **DataAccess** — EF Core mapping update for `DueDate` column; search handler updated for due date sorting
- **UI.Shared** — `WorkOrderManage` form updated with date picker; `WorkOrderSearch` page updated with column and overdue CSS indicator
- **Database** — New migration script adding nullable `DueDate` column to `WorkOrder` table

## Acceptance Criteria
### Unit Tests
- `WorkOrder_DueDate_ShouldDefaultToNull` — verify new work orders have no due date by default
- `WorkOrder_IsOverdue_WhenDueDatePastAndStatusNotComplete` — verify overdue detection logic
- `WorkOrder_IsNotOverdue_WhenDueDatePastAndStatusComplete` — verify completed work orders are not flagged overdue
- `WorkOrderManage_ShouldRenderDueDatePicker` — bUnit test verifying date picker appears on the form

### Integration Tests
- `WorkOrder_WithDueDate_ShouldPersistAndRetrieve` — save a work order with a due date and verify it round-trips through the database
- `WorkOrderSearchQuery_SortByDueDate_ShouldReturnOrderedResults` — verify search results sort correctly by due date

### Acceptance Tests
- Navigate to create work order form, set a due date, save, and verify the due date is displayed on the work order detail page
- Create a work order with a past due date, navigate to search, and verify the overdue visual indicator is present

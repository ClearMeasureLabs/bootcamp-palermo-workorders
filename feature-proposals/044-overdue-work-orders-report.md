## Why
Work orders that miss their due dates create operational risk and customer dissatisfaction. A dedicated overdue report enables managers to quickly identify and prioritize stalled work, reducing the number of items that slip through the cracks.

## What Changes
- Add `DueDate` property (nullable `DateOnly`) to the `WorkOrder` domain model in `src/Core/Model/`
- Add database migration script in `src/Database/scripts/Update/` to add the `DueDate` column to the `WorkOrder` table
- Update `DataContext` entity configuration in `src/DataAccess/` to map the new column
- Add `OverdueWorkOrdersQuery` to `src/Core/Queries/` returning work orders where `DueDate < today` and status is not Complete or Cancelled
- Add `OverdueWorkOrdersHandler` in `src/DataAccess/Handlers/`
- Add `OverdueWorkOrders.razor` page in `src/UI/Client/` with a sortable table showing overdue items
- Add API endpoint in `src/UI/Api/`
- Add navigation link to the report in the NavMenu

## Capabilities
### New Capabilities
- `DueDate` field on work orders, settable during creation and editing
- Dedicated overdue work orders report page listing all non-complete work orders past their due date
- Sortable columns: Work Order Number, Title, Room, Assignee, Due Date, Days Overdue
- Visual indicators (color coding) for severity based on days overdue

### Modified Capabilities
- Work order create and edit forms updated to include a `DueDate` date picker

## Impact
- `src/Core/` — modified `WorkOrder` model, new query class
- `src/DataAccess/` — updated entity configuration, new handler
- `src/Database/` — new migration script adding `DueDate` column
- `src/UI/Client/` — new report page, modified work order form components
- `src/UI/Api/` — new API endpoint
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `OverdueWorkOrdersHandler` returns only work orders with `DueDate` before today that are not Complete or Cancelled
- `OverdueWorkOrdersHandler` excludes work orders with no `DueDate` set
- `OverdueWorkOrdersHandler` excludes completed and cancelled work orders even if past due
- Days overdue calculation is accurate

### Integration Tests
- `OverdueWorkOrdersHandler` returns correct results from a seeded database with a mix of overdue, on-time, and completed work orders
- `DueDate` column persists correctly through EF Core

### Acceptance Tests
- Navigate to the overdue report page and verify overdue work orders are listed
- Create a work order with a past due date and verify it appears on the report
- Complete an overdue work order and verify it is removed from the report on refresh
- Verify the days overdue column shows the correct value

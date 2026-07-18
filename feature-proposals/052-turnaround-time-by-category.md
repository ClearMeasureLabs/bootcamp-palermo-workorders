## Why
Different categories of work requests (e.g., plumbing, electrical, HVAC) have inherently different complexity levels. Measuring turnaround time by category reveals which types consistently take longer, enabling better time estimates, resource allocation, and vendor management.

## What Changes
- Add `Category` property (string or enum) to the `WorkRequest` domain model in `src/Core/Model/`
- Add database migration script in `src/Database/scripts/Update/` to add the `Category` column to the `WorkRequest` table
- Update `DataContext` entity configuration in `src/DataAccess/`
- Add `TurnaroundTimeByCategoryQuery` to `src/Core/Queries/` returning a list of `(string Category, double AverageDays, int CompletedCount, double MinDays, double MaxDays)`
- Add `TurnaroundTimeByCategoryHandler` in `src/DataAccess/Handlers/`
- Add `TurnaroundByCategoryReport.razor` page in `src/UI/Client/` displaying a table and optional bar chart
- Update work request create and edit forms to include a Category dropdown
- Add API endpoint in `src/UI/Api/`
- Add navigation link in NavMenu under Reports

## Capabilities
### New Capabilities
- `Category` field on work requests with predefined options (e.g., Plumbing, Electrical, HVAC, General, Custodial)
- Report page showing average turnaround time per category
- Table columns: Category, Average Days, Min Days, Max Days, Completed Count
- Optional bar chart comparing categories visually

### Modified Capabilities
- Work request create and edit forms include a Category selector

## Impact
- `src/Core/` — modified `WorkRequest` model, new query class
- `src/DataAccess/` — updated entity configuration, new handler
- `src/Database/` — new migration script adding `Category` column
- `src/UI/Client/` — new report page, modified work request form components
- `src/UI/Api/` — new API endpoint
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `TurnaroundTimeByCategoryHandler` calculates correct averages per category
- `TurnaroundTimeByCategoryHandler` excludes non-completed work requests
- `TurnaroundTimeByCategoryHandler` handles categories with a single completed work request
- `TurnaroundTimeByCategoryHandler` returns empty results when no completed work requests exist

### Integration Tests
- `TurnaroundTimeByCategoryHandler` returns accurate per-category statistics from a seeded database
- `Category` column persists correctly through EF Core

### Acceptance Tests
- Navigate to the turnaround by category report and verify data is displayed
- Create work requests in different categories, complete them, and verify the report reflects accurate averages
- Verify the Category field appears on the work request create form

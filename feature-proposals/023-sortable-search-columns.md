## Why
Users browsing work order search results need to sort by different columns to quickly find relevant items, such as the most recent work orders, those assigned to a specific person, or those in a particular status. Sortable columns are a standard expectation for data grids.

## What Changes
- Add `SortColumn` and `SortDirection` properties to `WorkOrderSpecificationQuery` in `src/Core/Queries/`
- Define a `SortDirection` enum (Ascending, Descending) in `src/Core/`
- Update the query handler in `src/DataAccess/Handlers/` to apply dynamic ORDER BY based on the sort parameters
- Make column headers clickable on the `WorkOrderSearch` page in `src/UI/Client/`
- Add sort direction indicator (arrow icon) to active sort column
- Wire column header clicks to update the query and refresh results

## Capabilities
### New Capabilities
- Click column headers on WorkOrderSearch to sort by Number, Title, Status, Creator, or Assignee
- Toggle sort direction between ascending and descending on repeated clicks
- Visual indicator showing current sort column and direction

### Modified Capabilities
- WorkOrderSearch results grid headers become interactive clickable elements
- WorkOrderSpecificationQuery supports optional sort parameters

## Impact
- `src/Core/Queries/WorkOrderSpecificationQuery.cs` — new SortColumn and SortDirection properties
- `src/Core/` — new SortDirection enum
- `src/DataAccess/Handlers/` — updated query handler with dynamic ORDER BY logic
- `src/UI/Client/` — updated WorkOrderSearch page with clickable headers and sort indicators
- `src/UI/Api/` — updated API controller to accept sort parameters
- No database migration required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Handler sorts work orders by Number ascending when specified
- Handler sorts work orders by Number descending when specified
- Handler sorts work orders by Title, Status, Creator, and Assignee columns
- Default sort is by Number ascending when no sort parameters provided
- SortDirection enum has Ascending and Descending values

### Integration Tests
- Query returns work orders sorted by the specified column and direction from a seeded database
- Default sorting is applied when no sort parameters are provided

### Acceptance Tests
- User clicks the Number column header and results sort by Number ascending
- User clicks the same column header again and sort direction toggles to descending
- User clicks a different column header and results sort by that column ascending
- Active sort column displays a direction indicator arrow

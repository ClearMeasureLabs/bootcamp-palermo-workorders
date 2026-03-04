## Why
Rooms that generate a disproportionate number of work orders may have underlying maintenance issues requiring capital investment rather than repeated repairs. A room utilization report highlights these problem areas so facility managers can make informed budget and planning decisions.

## What Changes
- Add `RoomUtilizationQuery` to `src/Core/Queries/` returning a list of `(string RoomNumber, int WorkOrderCount, int OpenCount, int CompletedCount)` sorted by total count descending
- Add `RoomUtilizationHandler` in `src/DataAccess/Handlers/` grouping work orders by `RoomNumber` and computing counts
- Add `RoomUtilizationReport.razor` page in `src/UI/Client/` displaying a sortable table and optional bar chart
- Add API endpoint in `src/UI/Api/`
- Add navigation link in NavMenu under Reports

## Capabilities
### New Capabilities
- Report page showing work order counts per room number in a sortable table
- Columns: Room Number, Total Work Orders, Open (non-terminal), Completed, Cancelled
- Optional bar chart visualization of top rooms by volume
- Date range filter to scope the analysis

### Modified Capabilities
- None

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with EF Core group-by query
- `src/UI/Client/` — new report page
- `src/UI/Api/` — new API endpoint
- No database schema changes (uses existing `RoomNumber` column)
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `RoomUtilizationHandler` returns correct counts per room number
- `RoomUtilizationHandler` sorts results by total count descending
- `RoomUtilizationHandler` handles rooms with only completed or only open work orders
- `RoomUtilizationReport` component renders table rows for each room using bUnit

### Integration Tests
- `RoomUtilizationHandler` returns accurate room-level statistics from a seeded database
- Date range filter correctly scopes the results

### Acceptance Tests
- Navigate to the room utilization report and verify the table displays room data
- Verify sorting by clicking column headers
- Create a work order for a specific room and verify the count increases on refresh

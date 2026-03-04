## Why
Understanding creator activity patterns helps management identify high-volume requesters and assess whether work order quality correlates with creator behavior. A per-creator summary enables targeted training and process improvements for frequent requesters.

## What Changes
- Add `CreatorActivitySummaryQuery` to `src/Core/Queries/` returning a list of creator statistics: total created, completed count, cancelled count, completion rate percentage, and average resolution time
- Add `CreatorActivitySummaryHandler` in `src/DataAccess/Handlers/` grouping work orders by `Creator` and computing aggregates
- Add `CreatorActivitySummary.razor` page in `src/UI/Client/` displaying a sortable table of creator statistics
- Add API endpoint in `src/UI/Api/`
- Add navigation link in NavMenu under Reports

## Capabilities
### New Capabilities
- Report page showing per-creator statistics in a sortable table
- Columns: Creator Name, Total Created, Completed, Cancelled, Completion Rate (%), Average Resolution Time (days)
- Sortable by any column
- Optional date range filter to scope the statistics

### Modified Capabilities
- None

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with multi-aggregate EF Core query
- `src/UI/Client/` — new report page
- `src/UI/Api/` — new API endpoint
- No database schema changes (uses existing `Creator`, `CreatedDate`, `CompletedDate`, `Status` columns)
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `CreatorActivitySummaryHandler` returns correct totals per creator
- `CreatorActivitySummaryHandler` calculates completion rate accurately (completed / (completed + cancelled))
- `CreatorActivitySummaryHandler` calculates average resolution time only from completed work orders
- `CreatorActivitySummaryHandler` handles creators with zero completed work orders gracefully

### Integration Tests
- `CreatorActivitySummaryHandler` returns accurate per-creator statistics from a seeded database
- Date range filter correctly scopes results

### Acceptance Tests
- Navigate to the creator activity summary page and verify the table displays data
- Verify column sorting works for each column
- Create and complete a work order, then verify the creator's statistics update on refresh

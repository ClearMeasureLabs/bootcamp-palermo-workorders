## Why
Understanding the ratio of completed versus cancelled work orders over time reveals process health and helps identify periods where cancellation rates spike. A configurable time-period report empowers management to investigate root causes and improve completion rates.

## What Changes
- Add `CompletionRateQuery` to `src/Core/Queries/` accepting a time period parameter (week, month, quarter) and returning completed count, cancelled count, total count, and completion percentage
- Add `CompletionRateHandler` in `src/DataAccess/Handlers/` filtering work orders by `CompletedDate` or status change date within the selected period
- Add `CompletionRateReport.razor` page in `src/UI/Client/` displaying the metrics with a dropdown to select time period
- Add API endpoint in `src/UI/Api/`
- Add navigation link in NavMenu under a Reports section

## Capabilities
### New Capabilities
- Report page showing completion rate (completed / (completed + cancelled)) as a percentage
- Configurable time period selector: last 7 days, last 30 days, last 90 days, custom date range
- Display of raw counts alongside the percentage
- Visual indicator (progress bar or gauge) representing the completion rate

### Modified Capabilities
- None

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler
- `src/UI/Client/` — new report page
- `src/UI/Api/` — new API endpoint
- No database schema changes (uses existing `Status`, `CompletedDate` columns)
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `CompletionRateHandler` calculates correct percentage when both completed and cancelled work orders exist
- `CompletionRateHandler` returns 100% when all work orders in the period are completed
- `CompletionRateHandler` returns 0% when all work orders in the period are cancelled
- `CompletionRateHandler` returns null or N/A when no completed or cancelled work orders exist in the period
- `CompletionRateReport` component renders the percentage and counts correctly using bUnit

### Integration Tests
- `CompletionRateHandler` returns accurate results from a seeded database for each time period option
- Date filtering correctly includes only work orders within the selected range

### Acceptance Tests
- Navigate to the completion rate report and verify the default time period displays data
- Select each time period option and verify the displayed values update
- Verify the percentage matches the displayed completed and cancelled counts

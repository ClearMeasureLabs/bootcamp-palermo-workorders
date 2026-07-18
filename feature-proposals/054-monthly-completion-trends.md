## Why
Monthly completion trends reveal long-term patterns in work order throughput, helping leadership assess whether process improvements are taking effect and forecast future capacity needs. A 12-month view provides enough history to identify seasonal patterns.

## What Changes
- Add `MonthlyTrendQuery` to `src/Core/Queries/` returning a list of `(int Year, int Month, int CompletedCount)` for the last 12 months
- Add `MonthlyTrendHandler` in `src/DataAccess/Handlers/` grouping completed work orders by year and month of `CompletedDate`
- Add `MonthlyCompletionTrend.razor` component in `src/UI/Client/` rendering a line chart with month labels on the x-axis and completed counts on the y-axis
- Add API endpoint in `src/UI/Api/`
- Display the chart on the dashboard page

## Capabilities
### New Capabilities
- Line chart showing completed work order counts per month for the last 12 months
- X-axis labeled with month/year, y-axis with count
- Data point tooltips showing exact month and count
- Optional trend line overlay showing direction

### Modified Capabilities
- Dashboard page updated to include the monthly trend chart

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with EF Core date grouping
- `src/UI/Client/` — new chart component with JS interop
- `src/UI/Api/` — new API endpoint
- No database schema changes (uses existing `CompletedDate` column)
- May require a client-side charting library (e.g., Chart.js via JS interop) — requires approval per project conventions

## Acceptance Criteria
### Unit Tests
- `MonthlyTrendHandler` returns correct counts per month for known data
- `MonthlyTrendHandler` returns zero-count entries for months with no completions
- `MonthlyTrendHandler` returns exactly 12 months of data
- `MonthlyCompletionTrend` component renders the chart container using bUnit

### Integration Tests
- `MonthlyTrendHandler` returns accurate monthly counts from a seeded database with completions spanning multiple months
- Date range boundary (12 months back from today) is correctly calculated

### Acceptance Tests
- Navigate to the dashboard and verify the monthly trend chart is visible
- Verify 12 data points are displayed on the chart
- Hover over a data point and verify the tooltip shows the correct month and count

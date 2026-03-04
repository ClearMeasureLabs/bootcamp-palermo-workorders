## Why
Understanding work order creation trends over time helps facility managers anticipate demand and staff appropriately. A 30-day chart of daily creation counts reveals patterns such as day-of-week spikes or seasonal increases that inform resource planning.

## What Changes
- Add `CreatedPerDayQuery` to `src/Core/Queries/` accepting a `DateRange` parameter and returning a list of `(DateOnly Date, int Count)` tuples
- Add `CreatedPerDayHandler` in `src/DataAccess/Handlers/` grouping work orders by `CreatedDate` date component
- Add `WorkOrdersPerDayChart.razor` component in `src/UI/Client/` rendering a bar or line chart using a lightweight JavaScript chart library via JS interop
- Add API endpoint in `src/UI/Api/` returning the daily count data as JSON
- Include the chart on the dashboard page

## Capabilities
### New Capabilities
- Bar or line chart displaying work order creation counts per day for the last 30 days
- Hover tooltip showing exact count and date for each data point

### Modified Capabilities
- Dashboard page updated to include the chart component

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler
- `src/UI/Client/` — new chart component, potential JS interop for charting library
- `src/UI/Api/` — new API endpoint
- No database schema changes (uses existing `CreatedDate` column)
- May require a client-side charting library (e.g., Chart.js via JS interop) — requires approval per project conventions

## Acceptance Criteria
### Unit Tests
- `CreatedPerDayHandler` returns correct daily counts for a known dataset
- `CreatedPerDayHandler` returns zero-count entries for days with no work orders within the range
- `WorkOrdersPerDayChart` component renders the chart container element using bUnit

### Integration Tests
- `CreatedPerDayHandler` returns accurate results from a seeded database with work orders spread across multiple days
- Query respects the 30-day date range boundary

### Acceptance Tests
- Navigate to the dashboard and verify the chart is visible with labeled axes
- Create a work order and verify today's bar reflects the new count on page refresh
- Hover over a chart data point and verify the tooltip displays the correct date and count

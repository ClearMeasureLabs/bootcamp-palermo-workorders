## Why
Tracking average completion time gives operations teams a key performance indicator for evaluating service efficiency. Displaying this metric prominently on the dashboard enables data-driven decisions about process improvements and staffing.

## What Changes
- Add `AverageCompletionTimeQuery` to `src/Core/Queries/` returning a `TimeSpan` or nullable double representing average hours/days
- Add `AverageCompletionTimeHandler` in `src/DataAccess/Handlers/` calculating the average difference between `CompletedDate` and `CreatedDate` for completed work orders
- Add `AverageCompletionTime.razor` component in `src/UI/Client/` displaying the metric as a formatted card (e.g., "Avg. Completion: 3.2 days")
- Add API endpoint in `src/UI/Api/` to expose the metric
- Display the component on the dashboard page

## Capabilities
### New Capabilities
- Dashboard metric card showing average time from creation to completion for all completed work orders
- Option to filter by time period (last 7 days, 30 days, 90 days)

### Modified Capabilities
- Dashboard page updated to include the average completion time card

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with EF Core aggregation
- `src/UI/Client/` — new Razor component
- `src/UI/Api/` — new API endpoint
- No database schema changes (uses existing `CreatedDate` and `CompletedDate` columns)
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `AverageCompletionTimeHandler` returns correct average when multiple completed work orders exist
- `AverageCompletionTimeHandler` returns null or zero when no completed work orders exist
- `AverageCompletionTimeHandler` excludes non-completed work orders from the calculation
- `AverageCompletionTime` component renders the formatted time value using bUnit

### Integration Tests
- `AverageCompletionTimeHandler` computes accurate average from seeded completed work orders with known dates
- Handler correctly ignores cancelled and in-progress work orders

### Acceptance Tests
- Navigate to the dashboard and verify the average completion time metric is displayed
- Complete a work order and verify the metric updates on page refresh
- Verify the displayed value matches the expected average based on known test data

## Why
Operations managers need a quick overview of work order distribution across statuses to identify bottlenecks and allocate resources effectively. A dashboard widget showing counts grouped by status provides immediate situational awareness without navigating to filtered list views.

## What Changes
- Add `WorkOrderCountByStatusQuery` to `src/Core/Queries/` returning a dictionary of `WorkOrderStatus` to `int`
- Add `WorkOrderCountByStatusHandler` in `src/DataAccess/Handlers/` using EF Core `GroupBy` on `Status`
- Add `DashboardStatusCounts.razor` component in `src/UI/Client/` displaying status counts as styled cards or badges
- Add the component to the home page (`Index.razor`) or a new `Dashboard.razor` page
- Add API endpoint in `src/UI/Api/` to expose the query result

## Capabilities
### New Capabilities
- Dashboard widget displaying work order counts grouped by each `WorkOrderStatus` value (Draft, Assigned, InProgress, Complete, Cancelled)
- Clickable status cards that navigate to the work order list filtered by that status

### Modified Capabilities
- Home page layout updated to include the dashboard widget

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with EF Core aggregation query
- `src/UI/Client/` — new Razor component
- `src/UI/Api/` — new API endpoint
- No database schema changes required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `WorkOrderCountByStatusHandler` returns correct counts when work orders exist across multiple statuses
- `WorkOrderCountByStatusHandler` returns zero counts for statuses with no work orders
- `DashboardStatusCounts` component renders correct count values for each status using bUnit

### Integration Tests
- `WorkOrderCountByStatusHandler` returns accurate grouped counts from a seeded database
- Query returns all five status categories even when some have zero work orders

### Acceptance Tests
- Navigate to the dashboard page and verify all five status labels are visible
- Create a new work order and confirm the Draft count increments by one on page refresh
- Click a status card and verify navigation to the work order list filtered by that status

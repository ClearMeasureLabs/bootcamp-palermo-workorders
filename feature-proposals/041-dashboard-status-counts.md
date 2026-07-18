## Why
Operations managers need a quick overview of work request distribution across statuses to identify bottlenecks and allocate resources effectively. A dashboard widget showing counts grouped by status provides immediate situational awareness without navigating to filtered list views.

## What Changes
- Add `WorkRequestCountByStatusQuery` to `src/Core/Queries/` returning a dictionary of `WorkRequestStatus` to `int`
- Add `WorkRequestCountByStatusHandler` in `src/DataAccess/Handlers/` using EF Core `GroupBy` on `Status`
- Add `DashboardStatusCounts.razor` component in `src/UI/Client/` displaying status counts as styled cards or badges
- Add the component to the home page (`Index.razor`) or a new `Dashboard.razor` page
- Add API endpoint in `src/UI/Api/` to expose the query result

## Capabilities
### New Capabilities
- Dashboard widget displaying work request counts grouped by each `WorkRequestStatus` value (Draft, Assigned, InProgress, Complete, Cancelled)
- Clickable status cards that navigate to the work request list filtered by that status

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
- `WorkRequestCountByStatusHandler` returns correct counts when work requests exist across multiple statuses
- `WorkRequestCountByStatusHandler` returns zero counts for statuses with no work requests
- `DashboardStatusCounts` component renders correct count values for each status using bUnit

### Integration Tests
- `WorkRequestCountByStatusHandler` returns accurate grouped counts from a seeded database
- Query returns all five status categories even when some have zero work requests

### Acceptance Tests
- Navigate to the dashboard page and verify all five status labels are visible
- Create a new work request and confirm the Draft count increments by one on page refresh
- Click a status card and verify navigation to the work request list filtered by that status

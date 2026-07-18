## Why
Draft work orders awaiting assignment can be easily forgotten, leading to delayed service. A persistent notification badge in the navigation menu ensures managers are always aware of the unassigned backlog without needing to check a specific page.

## What Changes
- Add `UnassignedCountQuery` to `src/Core/Queries/` returning the count of work orders with Draft status
- Add `UnassignedCountHandler` in `src/DataAccess/Handlers/`
- Add `UnassignedBadge.razor` component in `src/UI/Client/` displaying a badge with the count next to the work orders navigation link
- Implement auto-refresh using a Blazor timer (e.g., polling every 60 seconds) to keep the count current
- Add API endpoint in `src/UI/Api/`
- Integrate the badge component into `NavMenu.razor`

## Capabilities
### New Capabilities
- Notification badge in the NavMenu showing the count of Draft (unassigned) work orders
- Auto-refresh of the count at a configurable interval (default 60 seconds)
- Badge hidden when count is zero
- Clicking the badge navigates to the work order list filtered by Draft status

### Modified Capabilities
- `NavMenu.razor` updated to include the unassigned badge component

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler
- `src/UI/Client/` — new badge component, modified NavMenu
- `src/UI/Api/` — new API endpoint
- No database schema changes required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `UnassignedCountHandler` returns correct count of Draft work orders
- `UnassignedCountHandler` excludes work orders in non-Draft statuses
- `UnassignedBadge` component renders the count when greater than zero using bUnit
- `UnassignedBadge` component hides when count is zero using bUnit

### Integration Tests
- `UnassignedCountHandler` returns accurate count from a seeded database

### Acceptance Tests
- Verify the unassigned badge is visible in the NavMenu when Draft work orders exist
- Create a new work order and verify the badge count increments
- Assign a Draft work order and verify the badge count decrements after refresh
- Verify the badge is hidden when no Draft work orders exist
- Click the badge and verify navigation to the filtered work order list

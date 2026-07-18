## Why
Users need real-time awareness of new notifications without manually refreshing the page. A live-updating badge count in the NavMenu ensures users promptly notice new events, reducing response times to work order assignments and status changes.

## What Changes
- Add `UnreadNotificationCountQuery` to `src/Core/Queries/` returning the integer count of unread notifications for the current user
- Add `UnreadNotificationCountHandler` in `src/DataAccess/Handlers/`
- Add `NotificationBadge.razor` component in `src/UI/Client/` displaying a numeric badge on the NavMenu notification link
- Implement periodic polling using a Blazor `Timer` (e.g., every 30 seconds) to refresh the count
- Alternatively, add SignalR hub in `src/UI/Server/` for real-time push updates when new notifications are created
- Add API endpoint in `src/UI/Api/` for the count query
- Integrate the badge into `NavMenu.razor`

## Capabilities
### New Capabilities
- Numeric badge on the NavMenu showing unread notification count for the logged-in user
- Auto-updating count via periodic polling (30-second interval) or SignalR real-time push
- Badge hidden when count is zero
- Badge animates briefly when count increases to draw attention

### Modified Capabilities
- `NavMenu.razor` updated to include the notification badge component

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler
- `src/UI/Client/` — new badge component, modified NavMenu, timer or SignalR client logic
- `src/UI/Server/` — optional SignalR hub registration
- `src/UI/Api/` — new API endpoint
- Depends on the `Notification` entity from feature 058 (or can be developed in parallel with a stub)
- If using SignalR: Microsoft.AspNetCore.SignalR is included in the ASP.NET Core framework, no additional NuGet package needed

## Acceptance Criteria
### Unit Tests
- `UnreadNotificationCountHandler` returns correct count for a given user
- `UnreadNotificationCountHandler` returns zero when all notifications are read
- `NotificationBadge` component renders the count when greater than zero using bUnit
- `NotificationBadge` component is hidden when count is zero using bUnit

### Integration Tests
- `UnreadNotificationCountHandler` returns accurate count from a seeded database with mixed read/unread notifications
- Count updates correctly after marking notifications as read

### Acceptance Tests
- Verify the notification badge appears in the NavMenu when unread notifications exist
- Trigger a new notification and verify the badge count increments within the polling interval
- Mark all notifications as read and verify the badge disappears
- Verify the badge is not visible for a user with no unread notifications

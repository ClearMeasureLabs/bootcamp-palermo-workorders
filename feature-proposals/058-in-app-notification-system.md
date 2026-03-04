## Why
Not all users check email promptly, and some notifications are too minor for email. An in-app notification system provides a lightweight, always-visible channel for communicating work order events, improving responsiveness and user engagement within the application itself.

## What Changes
- Add `Notification` entity to `src/Core/Model/` with properties: `Id` (Guid), `RecipientId` (Employee reference), `Message` (string), `IsRead` (bool), `CreatedDate` (DateTime), `LinkUrl` (optional string)
- Add database migration script in `src/Database/scripts/Update/` to create the `Notification` table
- Update `DataContext` in `src/DataAccess/` with `DbSet<Notification>` and entity configuration
- Add `CreateNotificationCommand` to `src/Core/` for persisting notifications
- Add `UnreadNotificationsQuery` and `MarkNotificationReadCommand` to `src/Core/`
- Add corresponding handlers in `src/DataAccess/Handlers/`
- Add `NotificationBell.razor` component in `src/UI/Client/` showing a bell icon with unread count badge in the application header
- Add `NotificationDropdown.razor` component displaying a list of recent notifications when the bell is clicked
- Add API endpoints in `src/UI/Api/` for fetching notifications and marking them as read
- Integrate the bell component into the main layout (`MainLayout.razor`)

## Capabilities
### New Capabilities
- `Notification` entity and database table for persisting in-app notifications
- Bell icon in the application header with unread count badge
- Dropdown list showing recent notifications with message, timestamp, and read/unread styling
- Click a notification to navigate to the related work order and mark it as read
- "Mark all as read" button in the dropdown
- API endpoints for listing unread notifications and marking individual or all notifications as read

### Modified Capabilities
- `MainLayout.razor` updated to include the notification bell in the header

## Impact
- `src/Core/` — new entity, new query and command classes
- `src/DataAccess/` — new entity configuration, new `DbSet`, new handlers
- `src/Database/` — new migration script creating `Notification` table with columns: Id, RecipientId, Message, IsRead, CreatedDate, LinkUrl
- `src/UI/Client/` — new bell and dropdown components, modified MainLayout
- `src/UI/Api/` — new API endpoints
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `CreateNotificationCommand` handler persists a notification with correct properties
- `UnreadNotificationsQuery` handler returns only unread notifications for the specified recipient
- `MarkNotificationReadCommand` handler sets `IsRead` to true
- `NotificationBell` component displays the correct unread count using bUnit
- `NotificationDropdown` component renders notification items with correct message text using bUnit

### Integration Tests
- Notification persists correctly through EF Core and can be retrieved by recipient
- Marking a notification as read updates the database record
- Unread count query returns accurate count after creating and reading notifications

### Acceptance Tests
- Verify the notification bell icon is visible in the application header
- Trigger a notification (e.g., assign a work order) and verify the unread count badge appears
- Click the bell and verify the dropdown shows the notification message
- Click a notification and verify navigation to the related work order
- Click "Mark all as read" and verify the badge disappears

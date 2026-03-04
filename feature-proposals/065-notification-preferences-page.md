## Why
Employees have different communication needs depending on their role and workload. A notification preferences page lets each employee control which events trigger notifications and how they receive them, reducing noise and improving the relevance of alerts.

## What Changes
- Add `NotificationPreference` entity in `src/Core/Model/` with properties: Id, EmployeeId, NotificationType (enum: Assignment, Completion, Overdue, Digest), Channel (enum: InApp, Email), IsEnabled
- Add database migration script to create `NotificationPreference` table
- Add `GetNotificationPreferencesQuery` in `src/Core/Queries/` returning preferences for a given employee
- Add `SaveNotificationPreferencesCommand` in `src/Core/Model/StateCommands/` to persist preference changes
- Add handlers for query and command in `src/DataAccess/Handlers/`
- Update `DataContext` in `src/DataAccess/` with `DbSet<NotificationPreference>`
- Add `NotificationPreferences.razor` page in `src/UI/Client/Pages/` with a grid of checkboxes (rows: notification types, columns: channels)
- Add route `/notification-preferences` and navigation link in `NavMenu.razor`
- Add `NotificationPreferenceSummary` DTO in `src/Core/Queries/` for API transport

## Capabilities
### New Capabilities
- Dedicated UI page for employees to view and edit their notification preferences
- Grid layout with notification types as rows and delivery channels as columns
- Toggle individual notifications on/off per channel
- Preferences persist across sessions via database storage
- Default preferences auto-created for new employees (all enabled via email)

### Modified Capabilities
- None

## Impact
- **Core**: New `NotificationPreference` entity, `NotificationType` enum, `Channel` enum, query/command objects
- **DataAccess**: Updated `DataContext`, new handlers for preferences CRUD
- **UI.Client**: New `NotificationPreferences.razor` page
- **UI.Shared**: New `NotificationPreferenceSummary` DTO
- **Database**: New migration script creating `NotificationPreference` table (Id, EmployeeId FK, NotificationType, Channel, IsEnabled)
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `SaveNotificationPreferencesCommand_WithValidPreferences_UpdatesDatabase` - command handler persists preference changes
- `GetNotificationPreferencesQuery_Handler_ReturnsAllPreferencesForEmployee` - handler returns complete preference set
- `GetNotificationPreferencesQuery_Handler_WithNoPreferences_ReturnsDefaults` - handler returns default-enabled preferences for new employees
- `NotificationPreferences_Component_RendersCheckboxGrid` - bUnit test confirming grid of checkboxes renders for each type/channel combination

### Integration Tests
- `NotificationPreference_RoundTrips_ThroughEfCore` - preferences save and load correctly
- `SaveNotificationPreferences_UpdatesExisting_WhenPreferenceExists` - updating an existing preference overwrites the record
- `GetNotificationPreferences_FiltersBy_EmployeeId` - query returns only the requesting employee's preferences

### Acceptance Tests
- Navigate to `/notification-preferences` and verify the preference grid renders with `data-testid="notification-preferences-grid"`
- Toggle a notification preference checkbox with `data-testid="pref-assignment-email"`, save, reload, and verify the state persists
- Verify all notification types (Assignment, Completion, Overdue, Digest) and channels (InApp, Email) are displayed as options

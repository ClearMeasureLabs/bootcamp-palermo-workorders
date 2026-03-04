## Why
Different employees have different communication preferences for receiving updates about work orders. Storing notification preferences per employee enables future notification features to deliver alerts through the right channels, reducing noise and ensuring important updates are not missed.

## What Changes
- Create a `NotificationPreference` entity in `src/Core/Model/` with properties: Id, EmployeeId, EventType (string enum: WorkOrderAssigned, WorkOrderCompleted, WorkOrderCancelled, StatusChanged), Channel (string enum: Email, InApp, None), IsEnabled (boolean)
- Create database migration scripts in `src/Database/scripts/Update/` to add the NotificationPreference table
- Add EF Core entity configuration in `src/DataAccess/` for the NotificationPreference entity
- Add a `NotificationPreferencesByEmployeeQuery` in `src/Core/Queries/`
- Create `UpdateNotificationPreferenceCommand` in `src/Core/Model/StateCommands/`
- Add MediatR handlers in `src/DataAccess/Handlers/` for querying and updating preferences
- Create a notification preferences section on the employee profile page in `src/UI/Client/` with toggles for each event type and channel
- Add API endpoints in `src/UI/Api/` for retrieving and updating notification preferences

## Capabilities
### New Capabilities
- NotificationPreference entity storing per-employee, per-event-type, per-channel settings
- Notification preferences UI section on employee profile page
- Toggle switches for each combination of event type and notification channel
- Support for event types: WorkOrderAssigned, WorkOrderCompleted, WorkOrderCancelled, StatusChanged
- Support for channels: Email, InApp, None

### Modified Capabilities
- Employee profile page enhanced with a notification preferences section

## Impact
- `src/Core/Model/NotificationPreference.cs` — new entity
- `src/Core/Model/StateCommands/` — new UpdateNotificationPreferenceCommand
- `src/Core/Queries/` — new NotificationPreferencesByEmployeeQuery
- `src/DataAccess/` — new EF Core configuration, new MediatR handlers
- `src/DataAccess/Handlers/` — new handlers for preference queries and updates
- `src/Database/scripts/Update/` — new migration script for NotificationPreference table
- `src/UI/Client/` — updated employee profile page with preferences UI
- `src/UI/Api/` — new endpoints for notification preferences
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- NotificationPreference entity stores EmployeeId, EventType, Channel, and IsEnabled correctly
- UpdateNotificationPreferenceCommand toggles IsEnabled for the specified preference
- NotificationPreferencesByEmployeeQuery returns all preferences for the specified employee
- Default preferences are created for a new employee with all events enabled for InApp channel
- bUnit test verifies the preferences section renders toggle switches for each event/channel pair

### Integration Tests
- Notification preferences persist correctly to the database
- Updating a preference changes the IsEnabled value in the database
- Query returns the correct preferences for a given employee from a seeded database
- Migration script creates the NotificationPreference table with correct schema

### Acceptance Tests
- User navigates to their profile and sees notification preference toggles
- User toggles a preference off and the change is saved
- User toggles a preference on and the change is saved
- Preference state persists across page reloads

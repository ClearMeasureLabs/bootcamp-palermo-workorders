## Why
Employees managing multiple work orders need a consolidated daily summary to plan their day without manually checking the system. A daily digest email reduces missed assignments and improves follow-through on in-progress work.

## What Changes
- Add `DigestEmailService` in `src/Core/` defining the interface for digest generation and delivery
- Add `DigestEmailHandler` in `src/DataAccess/Handlers/` that queries all Assigned and InProgress work orders grouped by assignee
- Add `DailyDigestBackgroundService` as a hosted service in `src/UI/Server/` that triggers the digest on a configurable schedule (default: 6:00 AM local time)
- Add `DigestEmailTemplate` Razor template in `src/UI/Server/EmailTemplates/` listing work order number, title, status, and room number
- Add `IEmailSender` interface in `src/Core/` and a `SmtpEmailSender` implementation in `src/UI/Server/`
- Add SMTP configuration section to `appsettings.json`
- Add `GetAssignedWorkOrdersForDigestQuery` in `src/Core/Queries/`

## Capabilities
### New Capabilities
- Scheduled daily digest email sent to each employee with Assigned or InProgress work orders
- Email contains a table of work order number, title, status, room number, and assigned date
- Configurable send time via `appsettings.json`
- Digest skipped for employees with no active work orders

### Modified Capabilities
- None

## Impact
- **Core**: New `IEmailSender` interface, `GetAssignedWorkOrdersForDigestQuery` query object
- **DataAccess**: New handler for digest query
- **UI.Server**: New hosted background service, email sender implementation, Razor email template
- **Configuration**: New SMTP settings in `appsettings.json`
- **Dependencies**: No new NuGet packages required (uses built-in `System.Net.Mail`)
- **Database**: No schema changes required

## Acceptance Criteria
### Unit Tests
- `DigestEmailHandler_WithAssignedWorkOrders_ReturnsGroupedByEmployee` - handler returns correct work orders grouped by assignee
- `DigestEmailHandler_WithNoActiveWorkOrders_ReturnsEmptyCollection` - handler returns empty when no Assigned/InProgress work orders exist
- `DigestEmailTemplate_WithWorkOrders_RendersAllFields` - template includes number, title, status, room for each work order
- `DailyDigestBackgroundService_AtScheduledTime_InvokesDigestHandler` - background service triggers at configured time

### Integration Tests
- `GetAssignedWorkOrdersForDigestQuery_ReturnsOnlyAssignedAndInProgress` - query filters out Draft, Complete, and Cancelled work orders
- `GetAssignedWorkOrdersForDigestQuery_GroupsByAssignee_ReturnsCorrectCounts` - each employee group contains only their assigned work orders
- `SmtpEmailSender_WithValidConfiguration_SendsEmail` - email sender connects and delivers message

### Acceptance Tests
- No direct UI acceptance tests (background email service); verify indirectly by confirming work orders in Assigned/InProgress status appear in the system and the digest endpoint can be triggered manually for testing

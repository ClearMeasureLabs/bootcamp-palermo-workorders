## Why
Employees managing multiple work requests need a consolidated daily summary to plan their day without manually checking the system. A daily digest email reduces missed assignments and improves follow-through on in-progress work.

## What Changes
- Add `DigestEmailService` in `src/Core/` defining the interface for digest generation and delivery
- Add `DigestEmailHandler` in `src/DataAccess/Handlers/` that queries all Assigned and InProgress work requests grouped by assignee
- Add `DailyDigestBackgroundService` as a hosted service in `src/UI/Server/` that triggers the digest on a configurable schedule (default: 6:00 AM local time)
- Add `DigestEmailTemplate` Razor template in `src/UI/Server/EmailTemplates/` listing work request number, title, status, and room number
- Add `IEmailSender` interface in `src/Core/` and a `SmtpEmailSender` implementation in `src/UI/Server/`
- Add SMTP configuration section to `appsettings.json`
- Add `GetAssignedWorkRequestsForDigestQuery` in `src/Core/Queries/`

## Capabilities
### New Capabilities
- Scheduled daily digest email sent to each employee with Assigned or InProgress work requests
- Email contains a table of work request number, title, status, room number, and assigned date
- Configurable send time via `appsettings.json`
- Digest skipped for employees with no active work requests

### Modified Capabilities
- None

## Impact
- **Core**: New `IEmailSender` interface, `GetAssignedWorkRequestsForDigestQuery` query object
- **DataAccess**: New handler for digest query
- **UI.Server**: New hosted background service, email sender implementation, Razor email template
- **Configuration**: New SMTP settings in `appsettings.json`
- **Dependencies**: No new NuGet packages required (uses built-in `System.Net.Mail`)
- **Database**: No schema changes required

## Acceptance Criteria
### Unit Tests
- `DigestEmailHandler_WithAssignedWorkRequests_ReturnsGroupedByEmployee` - handler returns correct work requests grouped by assignee
- `DigestEmailHandler_WithNoActiveWorkRequests_ReturnsEmptyCollection` - handler returns empty when no Assigned/InProgress work requests exist
- `DigestEmailTemplate_WithWorkRequests_RendersAllFields` - template includes number, title, status, room for each work request
- `DailyDigestBackgroundService_AtScheduledTime_InvokesDigestHandler` - background service triggers at configured time

### Integration Tests
- `GetAssignedWorkRequestsForDigestQuery_ReturnsOnlyAssignedAndInProgress` - query filters out Draft, Complete, and Cancelled work requests
- `GetAssignedWorkRequestsForDigestQuery_GroupsByAssignee_ReturnsCorrectCounts` - each employee group contains only their assigned work requests
- `SmtpEmailSender_WithValidConfiguration_SendsEmail` - email sender connects and delivers message

### Acceptance Tests
- No direct UI acceptance tests (background email service); verify indirectly by confirming work requests in Assigned/InProgress status appear in the system and the digest endpoint can be triggered manually for testing

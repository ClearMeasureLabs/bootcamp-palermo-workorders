## Why
Assignees need immediate awareness when a work order is assigned to them so they can begin planning and execution promptly. Email notification on assignment reduces response time and eliminates the need for manual follow-up by managers.

## What Changes
- Add `IEmailService` interface to `src/Core/` with a `SendAsync(string to, string subject, string body)` method
- Add `EmailService` implementation in `src/DataAccess/` or a new infrastructure project using SMTP or a mail provider SDK
- Add `AssignmentEmailNotificationHandler` in `src/DataAccess/Handlers/` implementing `INotificationHandler<WorkOrderAssignedNotification>` from MediatR
- Add `WorkOrderAssignedNotification` to `src/Core/` as a MediatR notification published after `DraftToAssignedCommand` executes
- Modify the `DraftToAssignedCommand` handler (or `StateCommandHandler`) to publish the notification after successful status change
- Add email configuration settings to `appsettings.json`
- Register `IEmailService` in the Lamar container

## Capabilities
### New Capabilities
- Email sent to the assignee's `EmailAddress` when a work order transitions from Draft to Assigned
- Email contains: work order number, title, description, room number, and a link to view the work order
- `IEmailService` abstraction for testability and future reuse

### Modified Capabilities
- `DraftToAssignedCommand` execution flow extended to publish a MediatR notification

## Impact
- `src/Core/` — new interface, new notification class
- `src/DataAccess/` — new notification handler, new email service implementation, modified state command flow
- `src/UI/Server/` — email configuration in `appsettings.json`, Lamar registration
- No database schema changes required
- May require an SMTP library or third-party email service SDK — requires approval per project conventions

## Acceptance Criteria
### Unit Tests
- `AssignmentEmailNotificationHandler` calls `IEmailService.SendAsync` with the assignee's email address
- `AssignmentEmailNotificationHandler` includes work order number and title in the email subject
- `AssignmentEmailNotificationHandler` includes work order details in the email body
- `AssignmentEmailNotificationHandler` handles null assignee email gracefully (no exception, logs warning)
- Notification is published when `DraftToAssignedCommand` executes successfully

### Integration Tests
- Assigning a work order triggers the notification handler with a stub email service
- Email service receives the correct parameters

### Acceptance Tests
- Assign a work order to an employee and verify an email is sent (using a test email interceptor or log verification)
- Verify the email content includes the work order number, title, and link

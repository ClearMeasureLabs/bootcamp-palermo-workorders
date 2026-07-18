## Why
Work order creators need to know when their requests are fulfilled so they can verify the work and close out any related processes. An automatic email notification on completion eliminates the need for creators to repeatedly check status and improves the feedback loop.

## What Changes
- Add `WorkOrderCompletedNotification` to `src/Core/` as a MediatR notification
- Add `CompletionEmailNotificationHandler` in `src/DataAccess/Handlers/` implementing `INotificationHandler<WorkOrderCompletedNotification>`
- Modify the `InProgressToCompleteCommand` handler (or `StateCommandHandler`) to publish the notification after successful status change
- Reuse `IEmailService` interface from the email-on-assignment feature (or add it if not yet present)
- Email template includes work order details and completion date

## Capabilities
### New Capabilities
- Email sent to the creator's `EmailAddress` when a work order transitions from InProgress to Complete
- Email contains: work order number, title, room number, assignee name, completion date, and a link to view the work order

### Modified Capabilities
- `InProgressToCompleteCommand` execution flow extended to publish a MediatR notification

## Impact
- `src/Core/` — new notification class (depends on `IEmailService` from feature 056 or adds it)
- `src/DataAccess/` — new notification handler, modified state command flow
- No database schema changes required
- No new NuGet packages required (reuses email infrastructure)

## Acceptance Criteria
### Unit Tests
- `CompletionEmailNotificationHandler` calls `IEmailService.SendAsync` with the creator's email address
- `CompletionEmailNotificationHandler` includes work order number and "Completed" in the email subject
- `CompletionEmailNotificationHandler` includes completion date and assignee name in the email body
- `CompletionEmailNotificationHandler` handles null creator email gracefully
- Notification is published when `InProgressToCompleteCommand` executes successfully

### Integration Tests
- Completing a work order triggers the notification handler with a stub email service
- Email service receives the creator's email and correct work order details

### Acceptance Tests
- Complete a work order and verify an email is sent to the creator (using a test email interceptor or log verification)
- Verify the email content includes the work order number, assignee, and completion date

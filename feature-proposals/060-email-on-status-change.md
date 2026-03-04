## Why
Keeping all stakeholders informed of work order progress reduces miscommunication and follow-up overhead. A generic email notification on any status change ensures both the creator and assignee are always aware of transitions without requiring separate notification handlers for each state change.

## What Changes
- Add `WorkOrderStatusChangedNotification` to `src/Core/` as a MediatR notification containing the work order, previous status, new status, and the user who made the change
- Add `StatusChangeEmailNotificationHandler` in `src/DataAccess/Handlers/` implementing `INotificationHandler<WorkOrderStatusChangedNotification>`
- Modify `StateCommandHandler` in `src/DataAccess/` to publish `WorkOrderStatusChangedNotification` after any successful state command execution
- Reuse `IEmailService` interface (from feature 056 or add it if not yet present)
- Send email to both creator and assignee (deduplicated if same person), with content tailored to the specific transition
- Add email templates or string formatting for each transition type

## Capabilities
### New Capabilities
- Email notification sent to both the work order creator and assignee on any status transition
- Email subject includes work order number and the new status
- Email body includes: work order title, previous status, new status, who made the change, timestamp, and a link to the work order
- Deduplication logic to avoid sending two emails when creator and assignee are the same person
- Configurable opt-out per notification type (future extensibility)

### Modified Capabilities
- `StateCommandHandler` extended to publish a generic status change notification after all state commands

## Impact
- `src/Core/` — new notification class
- `src/DataAccess/` — new notification handler, modified `StateCommandHandler`
- `src/UI/Server/` — email configuration (reuses existing if present)
- No database schema changes required
- No new NuGet packages required (reuses email infrastructure)

## Acceptance Criteria
### Unit Tests
- `StatusChangeEmailNotificationHandler` sends email to both creator and assignee
- `StatusChangeEmailNotificationHandler` deduplicates when creator and assignee are the same employee
- `StatusChangeEmailNotificationHandler` includes correct previous and new status in the email
- `StatusChangeEmailNotificationHandler` handles missing email addresses gracefully
- `StateCommandHandler` publishes `WorkOrderStatusChangedNotification` after each successful state command
- Email subject contains the work order number and new status name

### Integration Tests
- Executing any state command triggers the status change notification handler with a stub email service
- Notification contains the correct work order, previous status, and new status

### Acceptance Tests
- Transition a work order from Draft to Assigned and verify emails are sent to both creator and assignee
- Transition a work order from InProgress to Complete and verify the creator receives a completion email
- Verify email content includes the work order number, status transition, and link
- Verify no duplicate email is sent when creator and assignee are the same person

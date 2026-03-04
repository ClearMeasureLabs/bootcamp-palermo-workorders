## Why
Maintenance teams coordinate through Slack and need immediate visibility when work orders change status. Automated Slack notifications reduce response time for newly assigned work and keep stakeholders informed of progress without requiring them to check the application.

## What Changes
- Add `ISlackNotificationService` interface in `src/Core/Interfaces/` with method `SendStatusChangeNotification(WorkOrder workOrder, WorkOrderStatus previousStatus)`
- Add `SlackNotificationService` implementation in `src/DataAccess/Services/` that sends formatted messages via Slack Incoming Webhook
- Add `SlackSettings` configuration class with WebhookUrl, Channel, and Enabled properties
- Add `Slack` configuration section in `appsettings.json`
- Publish NServiceBus message from state command handlers on status transitions
- Add NServiceBus message handler in `src/Worker/` that invokes `ISlackNotificationService`
- Format Slack messages with work order number, title, old status, new status, and assignee

## Capabilities
### New Capabilities
- Automatic Slack message when a work order transitions between any statuses
- Configurable Slack webhook URL and channel via application settings
- Slack messages include work order number, title, previous status, new status, and assignee name
- Enable/disable Slack notifications via configuration without code changes

### Modified Capabilities
- State command handlers in `src/DataAccess/Handlers/` updated to publish notification messages after status transitions

## Impact
- **src/Core/Interfaces/** - New `ISlackNotificationService` interface
- **src/DataAccess/Services/** - New `SlackNotificationService` implementation
- **src/DataAccess/Handlers/** - Updated state command handlers to publish notification messages
- **src/Worker/** - New NServiceBus message handler for async Slack dispatch
- **src/UI/Server/appsettings.json** - New `Slack` configuration section
- **Dependencies** - No new NuGet packages; uses built-in `HttpClient` for webhook POST

## Acceptance Criteria
### Unit Tests
- `SlackNotification_StatusChange_SendsFormattedMessage` - Service sends POST request to configured webhook URL with correct JSON payload
- `SlackNotification_Disabled_DoesNotSendMessage` - When Slack.Enabled is false, no HTTP request is made
- `SlackNotification_MessageFormat_ContainsWorkOrderDetails` - Message body contains work order number, title, old status, and new status
- `SlackNotification_AssignedTransition_IncludesAssigneeName` - Message for Assigned status includes assignee's full name
- `SlackNotification_HttpFailure_LogsErrorWithoutThrowing` - HTTP failure from Slack is logged but does not propagate exception

### Integration Tests
- `SlackNotification_WorkOrderAssigned_PublishesNServiceBusMessage` - Assign a work order, verify NServiceBus message is published with correct data
- `SlackNotification_EndToEnd_DeliversToMockEndpoint` - Transition work order status, verify mock HTTP endpoint receives Slack webhook payload

### Acceptance Tests
- `SlackNotification_AssignWorkOrder_TriggersSlackMessage` - Assign a work order through the UI, verify mock Slack endpoint receives notification with correct work order number and assignee
- `SlackNotification_CompleteWorkOrder_TriggersSlackMessage` - Complete a work order through the UI, verify mock Slack endpoint receives notification with "Complete" status

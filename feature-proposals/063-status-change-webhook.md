## Why
External systems (facility management platforms, ticketing dashboards, mobile apps) need real-time notification when work order statuses change. Webhooks provide a standard, extensible integration point without requiring those systems to poll the API.

## What Changes
- Add `WebhookConfiguration` entity in `src/Core/Model/` with properties: Id, Url, EventType (enum: StatusChanged, Created, Assigned, Completed, Cancelled), IsActive, Secret (for HMAC signing), CreatedDate
- Add `IWebhookDispatcher` interface in `src/Core/` defining `DispatchAsync(WebhookEvent)` method
- Add `WebhookDispatcher` implementation in `src/DataAccess/` that sends HTTP POST with JSON payload and HMAC-SHA256 signature header
- Add `WebhookEvent` record in `src/Core/Model/` containing WorkOrderNumber, OldStatus, NewStatus, Timestamp, TriggeredBy
- Modify `StateCommandHandler` in `src/DataAccess/Handlers/` to invoke `IWebhookDispatcher` after successful status change
- Add database migration script to create `WebhookConfiguration` table
- Add `WebhookConfigurationQuery` and `SaveWebhookConfigurationCommand` in `src/Core/`
- Add handlers for webhook configuration CRUD in `src/DataAccess/Handlers/`
- Add `WebhookAdmin.razor` page in `src/UI/Client/Pages/` for managing webhook URLs

## Capabilities
### New Capabilities
- Fire HTTP POST webhook to configured URLs when work order status changes
- HMAC-SHA256 signature on webhook payload for receiver verification
- Admin UI page to add, edit, enable/disable, and delete webhook configurations
- Configurable event type filtering per webhook URL
- Retry with exponential backoff on failed webhook delivery (up to 3 attempts)

### Modified Capabilities
- `StateCommandHandler` extended to dispatch webhooks after state transitions

## Impact
- **Core**: New `WebhookConfiguration` entity, `WebhookEvent` record, `IWebhookDispatcher` interface, query/command objects
- **DataAccess**: New `WebhookDispatcher` service, modified `StateCommandHandler`, new CRUD handlers, updated `DataContext` with `DbSet<WebhookConfiguration>`
- **UI.Client**: New `WebhookAdmin.razor` page
- **Database**: New migration script adding `WebhookConfiguration` table (Id, Url, EventType, IsActive, Secret, CreatedDate)
- **Dependencies**: No new NuGet packages required (uses built-in `HttpClient`)

## Acceptance Criteria
### Unit Tests
- `WebhookDispatcher_WithActiveWebhook_SendsHttpPost` - dispatcher sends POST to configured URL
- `WebhookDispatcher_WithInactiveWebhook_SkipsDispatch` - dispatcher does not send for disabled webhooks
- `WebhookDispatcher_WithMatchingEventType_SendsPayload` - dispatcher sends only for matching event types
- `WebhookDispatcher_ComputesCorrectHmacSignature` - HMAC-SHA256 header matches expected value
- `StateCommandHandler_AfterStatusChange_InvokesWebhookDispatcher` - handler calls dispatcher after successful transition
- `WebhookEvent_Serialization_ContainsAllFields` - JSON payload includes work order number, old status, new status, timestamp

### Integration Tests
- `SaveWebhookConfiguration_PersistsToDatabase` - configuration round-trips through EF Core
- `WebhookConfigurationQuery_ReturnsActiveWebhooks` - query filters by IsActive flag
- `StateCommandHandler_WithWebhookConfigured_DispatchesAfterSave` - end-to-end flow from state command to webhook dispatch

### Acceptance Tests
- Navigate to webhook admin page, add a new webhook URL with `data-testid="webhook-url-input"`, select event type, save, and verify it appears in the list with `data-testid="webhook-list"`
- Edit an existing webhook configuration and verify changes persist after page reload
- Delete a webhook configuration and verify it is removed from the list

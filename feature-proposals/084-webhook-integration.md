## Why
External systems such as ticketing platforms, dashboards, and notification services need real-time awareness of work order events. A configurable webhook system enables push-based integration without requiring external systems to poll for changes, reducing latency and coupling.

## What Changes
- Add `WebhookSubscription` entity in `src/Core/Model/` with properties: Id, Url, Secret, EventTypes (comma-separated), IsActive, CreatedDate
- Add `WebhookPayload` record in `src/Core/Model/` representing the JSON structure sent to subscribers
- Add `WebhookSubscriptionConfiguration` EF Core entity configuration in `src/DataAccess/`
- Add database migration script in `src/Database/scripts/Update/` to create the `WebhookSubscription` table
- Add `IWebhookDispatcher` interface in `src/Core/Interfaces/` and `WebhookDispatcher` implementation in `src/DataAccess/Services/`
- Add `WebhookDispatcher` that sends HTTP POST with JSON payload and HMAC-SHA256 signature header to each active subscriber matching the event type
- Invoke `IWebhookDispatcher` from state command handlers after successful status transitions (Created, Assigned, Completed, Cancelled)
- Add NServiceBus message handler to dispatch webhooks asynchronously via the Worker project

## Capabilities
### New Capabilities
- Register webhook subscriptions with target URL, shared secret, and event type filters
- Dispatch JSON payloads via HTTP POST when work orders are created, assigned, completed, or cancelled
- Include HMAC-SHA256 signature in `X-Webhook-Signature` header for payload verification
- Asynchronous webhook delivery via NServiceBus to avoid blocking the main request pipeline

### Modified Capabilities
- State command handlers in `src/DataAccess/Handlers/` updated to publish webhook dispatch messages after status transitions

## Impact
- **src/Core/Model/** - New `WebhookSubscription` and `WebhookPayload` types
- **src/Core/Interfaces/** - New `IWebhookDispatcher` interface
- **src/DataAccess/** - New entity configuration, new service implementation, updated handlers
- **src/Database/** - New migration script for `WebhookSubscription` table
- **src/Worker/** - New NServiceBus message handler for async webhook dispatch
- **Dependencies** - No new NuGet packages; uses built-in `HttpClient`

## Acceptance Criteria
### Unit Tests
- `WebhookDispatcher_ActiveSubscription_SendsPostRequest` - Dispatcher sends HTTP POST to subscribed URL
- `WebhookDispatcher_InactiveSubscription_SkipsDispatch` - Inactive subscriptions are not called
- `WebhookDispatcher_EventTypeFilter_OnlySendsMatchingEvents` - Subscription for "Completed" does not receive "Assigned" events
- `WebhookPayload_Serialization_ContainsExpectedFields` - Payload JSON contains work order number, status, and timestamp
- `WebhookDispatcher_SignatureHeader_MatchesHmacOfBody` - X-Webhook-Signature header matches HMAC-SHA256 of request body using shared secret

### Integration Tests
- `Webhook_WorkOrderCreated_DispatchesPayload` - Create a work order, verify webhook dispatch message is published
- `Webhook_StatusTransition_DispatchesCorrectEventType` - Transition work order to Complete, verify payload event type is "Completed"
- `Webhook_MultipleSubscribers_AllReceivePayload` - Register two active subscriptions, verify both receive dispatched payloads

### Acceptance Tests
- `Webhook_CreateWorkOrder_ExternalEndpointReceivesPayload` - Create work order via UI, verify mock webhook endpoint receives POST with correct JSON structure
- `Webhook_CompleteWorkOrder_TriggersCompletedEvent` - Complete a work order through the UI workflow, verify webhook payload contains "Completed" event type

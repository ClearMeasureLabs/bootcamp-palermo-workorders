## Why

Users need a dedicated place to chat with the AI about work orders and related questions when no work order is selected. Today, the AI assistant (WorkOrderChat) only appears on the work order manage page when a specific work order with an assignee is selected. A full-page AI Agent chat gives a single, discoverable entry point for general work-order help using the same ChatClientFactory and tools.

## What Changes

- Add a new route (e.g. `/ai-agent`) with a full-page chat UI: full viewport height with input fixed at bottom (no browser scroll to reach it), send button, scrollable message history, loading indicator; Enter key submits the message (same as Send).
- Add a new backend query and handler: prompt plus current user identity (no work order), using ChatClientFactory and existing WorkOrderTool; general system prompt that includes the logged-in user so the AI is user-aware.
- Add a nav link to the AI Agent page for authenticated users.
- No changes to WorkOrderChat or work-order-manage flow.

## Capabilities

### New Capabilities

- `ai-agent-chat`: Full-page AI Agent chat screen; chat is aware of the logged-in user; user sends messages and receives AI responses via ChatClientFactory-backed handler with WorkOrderTool; no selected work order context.

### Modified Capabilities

- (none)

## Impact

- **LlmGateway:** New `AgentChatQuery` (prompt + user identity), `AgentChatHandler` (uses existing ChatClientFactory, WorkOrderTool; system prompt includes logged-in user).
- **UI.Shared:** New page component and route (page gets current user via UserSession and passes to query); NavMenu link.
- **UI.Server:** Handler registration if not auto-discovered.
- **Tests:** Unit test for handler; optional Playwright test for page.

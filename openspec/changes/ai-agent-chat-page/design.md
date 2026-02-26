## Context

The app already has WorkOrderChat: an embedded chat on the work order manage page that sends `WorkOrderChatQuery(Prompt, CurrentWorkOrder)` and uses `WorkOrderChatHandler` with `ChatClientFactory` and `WorkOrderTool`. Chat is only available when a work order is selected and has an assignee. The client uses `IBus` (RemotableBus) to send `IRemotableRequest` to the server; the server handles via MediatR and returns serialized responses. LlmGateway owns ChatClientFactory, WorkOrderChatQuery, and WorkOrderChatHandler; UI.Shared owns pages and NavMenu.

## Goals / Non-Goals

**Goals:**

- Provide a full-page AI Agent chat at a dedicated route (e.g. `/ai-agent`).
- Reuse ChatClientFactory and WorkOrderTool; no new LLM integrations.
- Use the same request/response pattern as WorkOrderChat (Bus.Send, IRemotableRequest, ChatResponse).
- Match UX patterns of WorkOrderChat (input, history, send, loading) for consistency.

**Non-Goals:**

- Changing WorkOrderChat or work-order-manage behavior.
- Persisting chat history across sessions.
- Adding new tools or changing LLM provider behavior.

## Decisions

- **New query/handler, no WorkOrder:** Introduce `AgentChatQuery(string Prompt, string UserName)` (or equivalent user identity) and `AgentChatHandler` instead of overloading WorkOrderChatQuery. Keeps work-order context separate and avoids optional parameters or branching in the existing handler. Alternative considered: optional WorkOrder on WorkOrderChatQuery — rejected to avoid coupling and confusion.

- **Chat is aware of logged-in user:** The page SHALL obtain the current user via `UserSession.GetCurrentUserAsync()` (same pattern as NavMenu, WorkOrderManage) and SHALL pass user identity (e.g. UserName) in AgentChatQuery. The handler SHALL include the current user in the system prompt (e.g. “The user you are helping is logged in as &lt;UserName&gt;”) so the AI responses can be user-aware. The server does not resolve user from HTTP context for this API; the client provides it so the contract stays explicit and serializable.

- **Same tools, different system prompt:** AgentChatHandler uses the same ChatOptions and WorkOrderTool (GetWorkOrderByNumber, GetAllEmployees) so the agent can still look up work orders and employees. System prompt is general (“help with work orders and related questions; you can look up work orders by number and list employees; be concise”) and SHALL include the logged-in user identity; no “current work order” lines. Alternative: separate tool set — rejected to avoid duplication and keep one source of truth for tools.

- **Full-page in UI.Shared:** New page component (e.g. AiAgent.razor) in UI.Shared with its own route; page gets current user via UserSession and passes it into the chat query; markup and styles modeled on WorkOrderChat (chat-container, chat-history, user/ai messages, loading). No event listener or work order state. Alternative: reuse WorkOrderChat with a “no work order” mode — rejected to keep the embedded component simple and avoid conditional UI branches.

- **Full-height layout, input always visible:** The chat SHALL use the full height of the viewport (e.g. 100vh or flex filling the page). The input area (text input + Send button) SHALL be fixed at the bottom so the user never has to scroll the browser to reach it. The message history area SHALL be the scrollable region in the remaining space above the input. Implement with CSS (e.g. flexbox: container flex column, history flex 1 min-height 0 overflow auto, input row fixed at bottom).

- **Enter key submits:** Pressing Enter in the message input SHALL submit the message (same as clicking Send). Handle via keydown/onkeydown (Enter without Shift) so the action is consistent with the Send button.

- **Nav link when authenticated:** Add NavMenu link to `/ai-agent` when `CurrentUser != null`, consistent with other work-order links. No new roles or permissions.

## Risks / Trade-offs

- **No conversation persistence:** Chat history is in-memory per page; refresh loses it. Mitigation: Acceptable for v1; persistence can be a later change.
- **Handler discovery:** If MediatR/Lamar do not scan LlmGateway, AgentChatHandler must be registered explicitly. Mitigation: Verify handler is invoked in development; add explicit registration if needed.

## Migration Plan

No data or API migration. Deploy new page and backend; nav link appears for authenticated users. Rollback: remove route and nav link; existing WorkOrderChat unchanged.

## Open Questions

- None.

## 1. Backend query and handler

- [x] 1.1 Add AgentChatQuery in LlmGateway: record with string Prompt, string UserName (or equivalent user identity), IRequest<ChatResponse>, IRemotableRequest (no WorkOrder parameter)
- [x] 1.2 Add AgentChatHandler in LlmGateway: takes ChatClientFactory and WorkOrderTool; builds general system prompt that includes the logged-in user (from query); no current work order; uses same ChatOptions/tools as WorkOrderChatHandler; calls GetChatClient() and GetResponseAsync; returns ChatResponse
- [x] 1.3 Ensure AgentChatHandler is registered (verify MediatR/Lamar scan of LlmGateway or add explicit registration in UI.Server)

## 2. AI Agent page and navigation

- [x] 2.1 Add AiAgent.razor in UI.Shared with route @page "/ai-agent": get current user via UserSession.GetCurrentUserAsync(); full-height chat layout (viewport height, input fixed at bottom, message history scrolls in remaining space); input + Send button; Enter key in input submits (same as Send); on Send call Bus.Send(AgentChatQuery(prompt, currentUser.UserName)); append user and AI messages; use data-testid/Elements for testability
- [x] 2.2 Add NavLink to /ai-agent in NavMenu.razor (e.g. "AI Agent" with icon, data-testid); show when CurrentUser != null

## 3. Tests

- [x] 3.1 Add unit test for AgentChatHandler: stub ChatClientFactory and WorkOrderTool; send AgentChatQuery with UserName; assert handler uses factory, builds system prompt that includes the user identity, no work order, returns ChatResponse
- [x] 3.2 (Optional) Add Playwright acceptance test for AI Agent page: navigate to /ai-agent, send message, assert chat history shows user and AI messages

# Lab 11: AI Web Chat for a Single Work Request

**Curriculum Section:** Section 08 (AI-Driven Development)
**Estimated Time:** 40 minutes
**Type:** Analyze + Build

---

## Objective

Explore how the application integrates AI chat into the work request management UI, allowing users to ask questions about a specific work request using context-aware LLM prompts with tool calling.

---

## Context

The application includes an AI chat feature embedded in the work request management page. When viewing a work request, users can ask natural language questions. The LLM receives the work request's full context (number, title, description, room, creator) and has access to tools for querying work requests and employees.

---

## Steps

### Step 1: Study the Chat Query

Open `src/LlmGateway/WorkRequestChatQuery.cs`:

```csharp
public record WorkRequestChatQuery(string Prompt, WorkRequest CurrentWorkRequest)
    : IRequest<ChatResponse>, IRemotableRequest
```

This is a standard MediatR request — the same CQRS pattern used for all operations. The `CurrentWorkRequest` provides context to the LLM.

### Step 2: Study the Chat Handler

Open `src/LlmGateway/WorkRequestChatHandler.cs`. Trace the flow:

1. System messages inject work request context (number, title, description, room, creator)
2. `ChatOptions.Tools` register callable functions: `GetWorkRequestByNumber`, `GetAllEmployees`
3. The LLM can call these tools to look up additional data during the conversation
4. Response is returned via `ChatResponse`

Note: The handler uses `ChatClientFactory` to obtain the Azure OpenAI client, with `TracingChatClient` wrapping it for OpenTelemetry distributed tracing.

### Step 3: Study the AI Tools

Open `src/LlmGateway/WorkRequestTool.cs`. These are the functions the LLM can invoke:

- `GetWorkRequestByNumber(string workRequestNumber)` — looks up any work request
- `GetAllEmployees()` — lists all employees with roles

The tools use `IBus.Send()` — the same bus as the rest of the application. The LLM interacts with the domain through the established CQRS architecture.

### Step 4: Study the Chat Client Factory

Open `src/LlmGateway/ChatClientFactory.cs`. Understand:
- How Azure OpenAI credentials are configured
- How `TracingChatClient` wraps the client for observability
- The `IsChatClientAvailable()` health check

### Step 5: Study the Blazor Component

Open `src/UI.Shared/Components/WorkRequestChat.razor`. Trace how:
- The component injects `IBus` and sends `WorkRequestChatQuery`
- User input becomes the `Prompt` parameter
- The current work request context is passed automatically
- Chat responses are displayed in the UI

### Step 6: Study the Integration Test

Open `src/IntegrationTests/LlmGateway/WorkRequestChatHandlerTests.cs`. See how the chat handler is tested with a real (or mocked) LLM backend.

### Step 7: Study the Acceptance Test

Open `src/AcceptanceTests/WorkRequests/WorkRequestAIChatTests.cs`. This test exercises the chat through the full UI — creating a work request, then using the chat feature to ask about it.

### Step 8: Trace the Architecture

```
User types question in WorkRequestChat.razor
    ↓
IBus.Send(WorkRequestChatQuery(prompt, currentWorkRequest))
    ↓
MediatR → WorkRequestChatHandler
    ↓
ChatClientFactory → Azure OpenAI (with TracingChatClient)
    ↓
LLM invokes tools: GetWorkRequestByNumber → IBus.Send(query) → DB
    ↓
ChatResponse returned to UI
```

The AI chat uses the **exact same** `IBus` and query infrastructure as the rest of the application.

---

## Expected Outcome

- Understanding of how LLM chat integrates into the CQRS architecture
- Knowledge of tool calling (function calling) with Azure OpenAI
- Understanding of the TracingChatClient observability pattern

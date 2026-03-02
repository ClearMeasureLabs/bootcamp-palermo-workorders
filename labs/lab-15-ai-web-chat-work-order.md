# Lab 15: AI Web Chat for a Single Work Order

**Curriculum Section:** Section 08 (AI-Driven Development)
**Estimated Time:** 40 minutes
**Type:** Analyze + Build

---

## Objective

Explore how the application integrates AI chat into the work order management UI, allowing users to ask questions about a specific work order using context-aware LLM prompts with tool calling.

---

## Context

The application includes an AI chat feature embedded in the work order management page. When viewing a work order, users can ask natural language questions. The LLM receives the work order's full context (number, title, description, room, creator) and has access to tools for querying work orders and employees.

---

## Steps

### Step 1: Study the Chat Query

Open `src/LlmGateway/WorkOrderChatQuery.cs`:

```csharp
public record WorkOrderChatQuery(string Prompt, WorkOrder CurrentWorkOrder)
    : IRequest<ChatResponse>, IRemotableRequest
```

This is a standard MediatR request — the same CQRS pattern used for all operations. The `CurrentWorkOrder` provides context to the LLM.

### Step 2: Study the Chat Handler

Open `src/LlmGateway/WorkOrderChatHandler.cs`. Trace the flow:

1. System messages inject work order context (number, title, description, room, creator)
2. `ChatOptions.Tools` register callable functions: `GetWorkOrderByNumber`, `GetAllEmployees`
3. The LLM can call these tools to look up additional data during the conversation
4. Response is returned via `ChatResponse`

Note: The handler uses `ChatClientFactory` to obtain the Azure OpenAI client, with `TracingChatClient` wrapping it for OpenTelemetry distributed tracing.

### Step 3: Study the AI Tools

Open `src/LlmGateway/WorkOrderTool.cs`. These are the functions the LLM can invoke:

- `GetWorkOrderByNumber(string workOrderNumber)` — looks up any work order
- `GetAllEmployees()` — lists all employees with roles

The tools use `IBus.Send()` — the same bus as the rest of the application. The LLM interacts with the domain through the established CQRS architecture.

### Step 4: Study the Chat Client Factory

Open `src/LlmGateway/ChatClientFactory.cs`. Understand:
- How Azure OpenAI credentials are configured
- How `TracingChatClient` wraps the client for observability
- The `IsChatClientAvailable()` health check

### Step 5: Study the Blazor Component

Open `src/UI.Shared/Components/WorkOrderChat.razor`. Trace how:
- The component injects `IBus` and sends `WorkOrderChatQuery`
- User input becomes the `Prompt` parameter
- The current work order context is passed automatically
- Chat responses are displayed in the UI

### Step 6: Study the Integration Test

Open `src/IntegrationTests/LlmGateway/WorkOrderChatHandlerTests.cs`. See how the chat handler is tested with a real (or mocked) LLM backend.

### Step 7: Study the Acceptance Test

Open `src/AcceptanceTests/WorkOrders/WorkOrderAIChatTests.cs`. This test exercises the chat through the full UI — creating a work order, then using the chat feature to ask about it.

### Step 8: Trace the Architecture

```
User types question in WorkOrderChat.razor
    ↓
IBus.Send(WorkOrderChatQuery(prompt, currentWorkOrder))
    ↓
MediatR → WorkOrderChatHandler
    ↓
ChatClientFactory → Azure OpenAI (with TracingChatClient)
    ↓
LLM invokes tools: GetWorkOrderByNumber → IBus.Send(query) → DB
    ↓
ChatResponse returned to UI
```

The AI chat uses the **exact same** `IBus` and query infrastructure as the rest of the application.

---

## Expected Outcome

- Understanding of how LLM chat integrates into the CQRS architecture
- Knowledge of tool calling (function calling) with Azure OpenAI
- Understanding of the TracingChatClient observability pattern

---

## Discussion Questions

1. The chat handler injects work order context via system messages. What happens if the context is too large? How would you handle a work order with a 4000-character description?
2. The LLM has access to `GetWorkOrderByNumber` and `GetAllEmployees` tools. What would you add? What should remain off-limits?
3. How does `TracingChatClient` help diagnose production issues with AI features?
4. The chat uses the same `IBus.Send()` as the UI. Why is this important for consistency and security?

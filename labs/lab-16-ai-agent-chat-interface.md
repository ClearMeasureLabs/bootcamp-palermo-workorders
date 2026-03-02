# Lab 16: AI Agent Chat Interface to Control the Application

**Curriculum Section:** Section 08 (AI-Driven Development - MCP & AI Agents)
**Estimated Time:** 45 minutes
**Type:** Analyze + Build

---

## Objective

Explore how an AI agent can control the entire application through conversational prompts — creating work orders, assigning them, and managing the full lifecycle — using the MCP server and Application Chat interface.

---

## Context

While Lab 15 covers AI chat scoped to a single work order, this lab explores the **Application Chat** — a general-purpose AI assistant that can execute any operation. Combined with the MCP server, an external AI agent can manage the full work order lifecycle through natural language.

---

## Steps

### Step 1: Study the Application Chat Handler

Open `src/LlmGateway/ApplicationChatHandler.cs`:

```csharp
public class ApplicationChatHandler(ChatClientFactory factory, IToolProvider toolProvider)
    : IRequestHandler<ApplicationChatQuery, ChatResponse>
```

Key differences from `WorkOrderChatHandler`:
- Uses `IToolProvider.GetTools()` — **all** MCP tools, not just two
- Maintains chat history across the conversation
- Knows the currently logged-in user
- System prompt is general-purpose: "helpful AI assistant for a work order management application"

### Step 2: Study the Tool Provider

Open `src/McpServer/ToolProvider.cs`. This wraps all MCP tools as `AITool` instances:

- `ListWorkOrders(status?)` — filter by status
- `GetWorkOrder(workOrderNumber)` — full details
- `CreateWorkOrder(title, description, creatorUsername, roomNumber?)` — create draft
- `ExecuteWorkOrderCommand(workOrderNumber, commandName, executingUsername, assigneeUsername?)` — state transitions
- `UpdateWorkOrderDescription(workOrderNumber, newDescription, updatingUsername)` — edit
- `ListEmployees()` — all staff
- `GetEmployee(username)` — single employee

### Step 3: Study the Application Chat Page

Open `src/UI.Shared/Pages/ApplicationChat.razor`. Trace how:
- Chat history is maintained in a list
- Each message is sent with full conversation context
- The AI can execute multi-step operations (create + assign in one conversation)

### Step 4: Study the MCP Conversation Test

Open `src/AcceptanceTests/McpServer/McpChatConversationTests.cs`. This test proves an AI agent can execute a multi-step workflow from a single natural language prompt:

```csharp
var response = await _helper!.SendPrompt(
    "I am Timothy Lovejoy (my username is tlovejoy). " +
    "Create a new work order assigned to Groundskeeper Willie (username gwillie) " +
    "to cut the grass...");
```

The test then verifies the work order was actually created and assigned in the database. The AI agent:
1. Parsed the natural language request
2. Called `create-work-order` with extracted parameters
3. Took the returned work order number
4. Called `execute-work-order-command` with `DraftToAssignedCommand`
5. Reported the results

### Step 5: Study the MCP Lifecycle Test

Open `src/AcceptanceTests/McpServer/McpWorkOrderLifecycleTests.cs`. This test exercises the full lifecycle via direct MCP tool calls (without LLM):

```csharp
var createResult = await _helper!.CallToolDirectly("create-work-order", ...);
var assignResult = await _helper!.CallToolDirectly("execute-work-order-command", ...);
var beginResult = await _helper!.CallToolDirectly("execute-work-order-command", ...);
var completeResult = await _helper!.CallToolDirectly("execute-work-order-command", ...);
```

Compare this to the Playwright acceptance test from Lab 12 — same workflow, different entry point.

### Step 6: Study the MCP Test Helper

Open `src/AcceptanceTests/McpServer/McpTestHelper.cs`. Understand:
- `ConnectAsync()` — connects to the MCP HTTP endpoint at `/mcp`
- `CallToolDirectly(name, args)` — invokes a tool without LLM
- `SendPrompt(prompt)` — sends a prompt to the LLM with all MCP tools available
- Tools are discovered dynamically via `ListToolsAsync()`

### Step 7: Compare Three Entry Points

The same Draft → Assigned → InProgress → Complete workflow can be driven through:

| Entry Point | Test File | Uses Browser? | Uses LLM? |
|-------------|-----------|---------------|-----------|
| Blazor UI | `WorkOrderFullLifecycleTests.cs` | Yes | No |
| MCP Direct | `McpWorkOrderLifecycleTests.cs` | No | No |
| MCP + LLM | `McpChatConversationTests.cs` | No | Yes |

All three exercise the **same** `IBus.Send()` → MediatR → Handler → Database pipeline.

---

## Expected Outcome

- Understanding of how AI agents control applications via MCP tools
- Comparison of three entry points (UI, MCP direct, MCP + LLM)
- Understanding of multi-step AI agent conversations

---

## Discussion Questions

1. The MCP conversation test uses a single prompt to trigger create + assign. What if the LLM misinterprets the request? How does the state command validation (`IsValid()`) serve as a safety net?
2. Compare the three entry points for the same workflow. Which gives the most confidence? Which is fastest? Which is most flexible?
3. The `ApplicationChatHandler` passes the current username to the LLM. Why is this important for authorization in tool calls?
4. What operations should an AI agent NOT be able to perform? How would you restrict the tool set?
5. How does the "Let's delegate a feature to the computer" concept from the curriculum apply here?

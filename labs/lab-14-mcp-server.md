# Lab 14: MCP Server - AI Tool Integration

**Curriculum Section:** Section 08 (AI-Driven Development - Model Context Protocol)
**Estimated Time:** 45 minutes
**Type:** Analyze + Build

---

## Objective

Explore the Model Context Protocol (MCP) server and understand how AI agents interact with the application through structured tools. See how the existing CQRS architecture naturally maps to MCP tool definitions.

---

## Context

The **Model Context Protocol (MCP)** is a standard for connecting AI models to external tools and data sources. This project includes a full MCP server that exposes work order operations as tools that AI agents can call.

The MCP server bridges the AI world with the application's CQRS architecture:
```
AI Agent → MCP Tool → IBus.Send(Command/Query) → MediatR → Handler → Database
```

---

## Steps

### Step 1: Explore the MCP Work Order Tools

Open `src/McpServer/Tools/WorkOrderTools.cs`. List all tools:

| Tool Name | MCP Method | Underlying CQRS Operation |
|-----------|-----------|---------------------------|
| `list-work-orders` | `McpServerTool` | `WorkOrderSpecificationQuery` |
| `get-work-order` | `McpServerTool` | `WorkOrderByNumberQuery` |
| `create-work-order` | `McpServerTool` | `SaveDraftCommand` |
| `assign-work-order` | `McpServerTool` | `DraftToAssignedCommand` |
| `begin-work-order` | `McpServerTool` | `AssignedToInProgressCommand` |
| `complete-work-order` | `McpServerTool` | `InProgressToCompleteCommand` |

For each tool, note:
- Input parameters (e.g., `workOrderNumber`, `assigneeUsername`)
- How it constructs the underlying command/query
- What it returns to the AI agent

### Step 2: Explore the MCP Employee Tools

Open `src/McpServer/Tools/EmployeeTools.cs`. List the tools:

| Tool Name | Underlying Operation |
|-----------|---------------------|
| `list-employees` | `EmployeeGetAllQuery` |
| `get-employee` | `EmployeeByUserNameQuery` |

### Step 3: Explore MCP Resources

Open `src/McpServer/Resources/ReferenceResources.cs`. These expose static reference data to AI agents — like architecture documentation, workflow descriptions, or system metadata.

### Step 4: Study MCP Integration Tests

Open `src/IntegrationTests/McpServer/McpWorkOrderToolTests.cs`. Study the test pattern:

- How the MCP server is instantiated in tests
- How tools are called programmatically
- How results are verified

### Step 5: Study the MCP Lifecycle Tests

Open `src/AcceptanceTests/McpServer/McpWorkOrderLifecycleTests.cs`. This test exercises the full work order lifecycle through MCP tools:

1. Create a work order (MCP `create-work-order`)
2. Assign it (MCP `assign-work-order`)
3. Begin work (MCP `begin-work-order`)
4. Complete it (MCP `complete-work-order`)
5. Verify the final state

Note how this mirrors the UI acceptance test from Lab 11, but driven by AI tools instead of a browser.

### Step 6: Write a New MCP Integration Test

Add a test to the MCP integration tests that verifies the `list-work-orders` tool returns work orders filtered by status.

The test should:
1. Create test employees and work orders in different statuses
2. Call the MCP `list-work-orders` tool with a status filter
3. Verify only work orders with the requested status are returned

Use the existing test patterns as your guide.

### Step 7: Trace the Architecture

Draw the full flow from AI agent to database:

```
AI Agent (Claude/GPT)
    ↓ MCP Protocol (JSON-RPC)
MCP Server
    ↓ Tool Handler
WorkOrderTools.cs
    ↓ IBus.Send()
MediatR Pipeline
    ↓ Handler
StateCommandHandler / QueryHandler
    ↓ EF Core
DataContext → SQL Server
```

Compare this to the UI flow from Lab 02:
```
User → Blazor UI → SingleApiController → IBus.Send() → MediatR → Handler → DB
```

The layers from `IBus.Send()` downward are **identical**. The MCP server is just another entry point into the same CQRS architecture.

---

## Expected Outcome

- Understanding of how MCP tools map to CQRS commands and queries
- Understanding of how the Onion Architecture enables multiple entry points (UI, API, MCP)
- A new passing MCP integration test

---

## Discussion Questions

1. The MCP tools call the same `IBus.Send()` as the UI. What architectural principle makes this possible? (Onion Architecture — the core is entry-point agnostic)
2. How do MCP tools enable AI agents to interact with enterprise applications **safely**? What prevents an AI from corrupting data? (State command validation: `IsValid()` checks status + user permissions)
3. Compare the MCP lifecycle test to the Playwright acceptance test. Which is faster? Which provides more confidence? Why?
4. The curriculum mentions "10-Tool Analysis" and "Let's delegate a feature to the computer." How do MCP tools enable this delegation?
5. What new MCP tools would you add to this application? What operations should remain human-only?

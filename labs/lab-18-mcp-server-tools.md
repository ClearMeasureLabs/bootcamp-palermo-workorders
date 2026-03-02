# Lab 18: MCP Server - AI Tool Integration

**Curriculum Section:** Section 08 (AI-Driven Development - Model Context Protocol)
**Estimated Time:** 45 minutes
**Type:** Analyze + Build

---

## Objective

Explore the Model Context Protocol server and understand how the CQRS architecture naturally maps to MCP tool definitions.

---

## Steps

### Step 1: Explore Work Order Tools

Open `src/McpServer/Tools/WorkOrderTools.cs`. Map each tool to its underlying CQRS operation:

| MCP Tool | CQRS Operation |
|----------|---------------|
| `list-work-orders` | `WorkOrderSpecificationQuery` |
| `get-work-order` | `WorkOrderByNumberQuery` |
| `create-work-order` | `SaveDraftCommand` |
| `assign-work-order` | `DraftToAssignedCommand` |
| `begin-work-order` | `AssignedToInProgressCommand` |
| `complete-work-order` | `InProgressToCompleteCommand` |

### Step 2: Explore Employee Tools

Open `src/McpServer/Tools/EmployeeTools.cs`.

### Step 3: Explore MCP Resources

Open `src/McpServer/Resources/ReferenceResources.cs` — static reference data for AI agents.

### Step 4: Study Integration Tests

Open `src/IntegrationTests/McpServer/McpWorkOrderToolTests.cs` — how tools are tested.

### Step 5: Study the Lifecycle Test

Open `src/AcceptanceTests/McpServer/McpWorkOrderLifecycleTests.cs` — full lifecycle through MCP.

### Step 6: Write a New MCP Test

Add a test that creates a work order via MCP `create-work-order`, retrieves it via `get-work-order`, and verifies fields match.

### Step 7: Trace the Architecture

```
AI Agent → MCP Tool → IBus.Send(Command/Query) → MediatR → Handler → Database
```

The layers from `IBus.Send()` downward are **identical** to the UI path. MCP is just another entry point.

---

## Expected Outcome

- Understanding of how MCP tools map to CQRS commands/queries
- A new passing MCP integration test

---

## Discussion Questions

1. Why does the Onion Architecture make multiple entry points (UI, API, MCP) possible?
2. How do state command validations protect against AI agents corrupting data?
3. What new MCP tools would you add? What should remain human-only?

## Context

The ChurchBulletin system follows Onion Architecture with a CQRS pattern via MediatR. Domain operations flow through an `IBus` abstraction that wraps MediatR. Queries (`IRemotableRequest`) and state commands (`IStateCommand`) handle all domain interactions. The `LlmGateway` project already defines a `WorkOrderTool` class with `[Description]`-annotated methods that wrap `IBus` queries — this establishes a precedent for tool-style interfaces over domain operations.

The Web API uses a single-endpoint pattern (`SingleApiController`) that deserializes `WebServiceMessage` objects by fully-qualified type name and routes them through `IBus`. The existing architecture cleanly supports adding new outer-layer projects that depend inward on Core and DataAccess.

## Goals / Non-Goals

**Goals:**
- Expose ChurchBulletin domain operations via the Model Context Protocol, enabling AI agents to query and manage work orders and employees
- Reuse existing MediatR queries and state commands through `IBus` — no duplication of business logic
- Follow Onion Architecture: MCP server sits in the outer layer alongside UI
- Support stdio transport for local AI agent usage (e.g., Claude Code, Cursor)
- Provide a standalone host process that can run independently of the Blazor server

**Non-Goals:**
- SSE/HTTP transport in the initial implementation (stdio is sufficient for local AI tools)
- Authentication or authorization on the MCP server (initial version is local-only via stdio)
- Replacing the existing Web API or Blazor UI
- Modifying Core or DataAccess projects
- AI chat or prompt management (that remains in LlmGateway)

## Decisions

### Decision 1: Use the official C# MCP SDK (`ModelContextProtocol` NuGet package)

**Rationale:** The `ModelContextProtocol` package (from the official MCP C# SDK) provides first-class .NET support for hosting MCP servers with tool and resource registration. It integrates with `Microsoft.Extensions.Hosting` and standard DI, which aligns with the project's existing patterns.

**Alternatives considered:**
- Hand-rolling MCP JSON-RPC handling: Too much protocol work with no benefit
- Using `Microsoft.Extensions.AI` alone: Provides tool abstractions but not the MCP transport layer

### Decision 2: Standalone console host using `Microsoft.Extensions.Hosting`

**Rationale:** An MCP server using stdio transport needs its own process. A generic host (`Host.CreateDefaultBuilder`) with `AddMcpServer().WithStdioTransport()` is the idiomatic approach. This keeps the MCP server decoupled from the Blazor server while sharing the same DI registration patterns.

**Alternatives considered:**
- Embedding in UI.Server: Would couple MCP to the web host lifecycle and complicate stdio transport
- Worker service: Unnecessary — generic host is sufficient

### Decision 3: Register MediatR handlers and `IBus` using the same Lamar/DI patterns as UIServiceRegistry

**Rationale:** The MCP server needs access to the same MediatR handlers and `IBus` to execute domain operations. Reusing the registration pattern from `UIServiceRegistry` ensures consistency and avoids handler duplication.

### Decision 4: One tool class per domain area, mirroring `WorkOrderTool` in LlmGateway

**Rationale:** The existing `WorkOrderTool` pattern (methods with `[Description]` attributes calling `IBus`) is a clean precedent. The MCP server will define similar tool classes registered as MCP tools. This keeps tool definitions cohesive and testable.

### Decision 5: MCP resources for static reference data (statuses, roles)

**Rationale:** Work order statuses and roles are static value objects. Exposing them as MCP resources (read-only) rather than tools is semantically correct per the MCP spec — resources represent data the client can read, while tools represent actions.

### Decision 6: Full system acceptance tests using MCP client SDK + IChatClient + LLM

**Rationale:** The MCP server must be tested end-to-end the way a real AI client would use it: launch the server as a subprocess, connect via stdio, discover tools, and route natural-language prompts through an LLM that calls the tools. This validates the full stack — MCP protocol, DI, database, and tool execution — in a single test.

**Architecture:**

```
[NUnit Test] → [IChatClient (Azure OpenAI)]
                    ↓ (function invocation)
              [McpClientTool (from MCP SDK)]
                    ↓ (stdio JSON-RPC)
              [McpServer process (child process)]
                    ↓ (IBus → MediatR → EF Core)
              [Database (LocalDB/SQLite)]
```

**Key components:**

1. **`McpServerFixture`** (`[SetUpFixture]`): Starts the MCP server as a child process using `StdioClientTransport` from the `ModelContextProtocol` NuGet package. Creates an `McpClient` and caches the tool list. Disposes client and kills process on teardown. Similar lifecycle to the existing `ServerFixture` for the Blazor server.

2. **`McpAcceptanceTestBase`**: Base class providing access to the `McpClient`, `IList<McpClientTool>`, and a configured `IChatClient` with MCP tools wired in. Builds the chat client using the same `ChatClientFactory` pattern (Azure OpenAI). Wraps `UseFunctionInvocation()` so the LLM automatically calls MCP tools.

3. **Test flow:**
   ```csharp
   // Fixture (once)
   var transport = new StdioClientTransport(new StdioClientTransportOptions
   {
       Name = "ChurchBulletin",
       Command = "dotnet",
       Arguments = ["run", "--project", "../../../../McpServer"],
   });
   var mcpClient = await McpClient.CreateAsync(transport);
   var tools = await mcpClient.ListToolsAsync();

   // Per-test
   var chatClient = azureOpenAiClient.AsBuilder().UseFunctionInvocation().Build();
   var response = await chatClient.GetResponseAsync(
       [new ChatMessage(ChatRole.User, "List all work orders")],
       new ChatOptions { Tools = [.. tools] });
   // Assert response contains expected data
   ```

4. **Database setup:** Reuses `TestHost` from IntegrationTests for database seeding and `IBus` for verification queries. The MCP server connects to the same database via shared connection string configuration.

5. **LLM availability:** Tests are marked `[Explicit]` and catch connection failures with `Assert.Inconclusive` so they don't break CI when no LLM is available.

**Alternatives considered:**
- Testing MCP tools without an LLM (direct `McpClient.CallToolAsync`): Validates protocol but misses the AI integration layer. Already covered by integration tests.
- Using the Blazor UI + Playwright for MCP testing: Wrong abstraction. MCP is a programmatic protocol, not a UI.

## Risks / Trade-offs

- **[NuGet dependency]** Adding the `ModelContextProtocol` package is a new dependency. → Mitigation: It is the official MCP C# SDK and is MIT-licensed. Approval required per project rules.
- **[Database connection]** The MCP server needs a database connection string for EF Core. → Mitigation: Use the same `appsettings.json` / environment variable pattern as the UI server. For local dev, point to LocalDB.
- **[Process lifecycle]** Stdio MCP servers are started/stopped by the AI client. → Mitigation: Standard .NET generic host handles graceful shutdown. No long-running background tasks needed.
- **[State command complexity]** State commands have preconditions (valid transitions, authorization). → Mitigation: Reuse existing `IStateCommand.IsValid()` and status validation. Return clear error messages in MCP tool responses when preconditions fail.

- **[LLM non-determinism]** LLM responses vary between runs; the same prompt may produce different tool call sequences. → Mitigation: Assert on data presence in the response (e.g., "response contains work order number X") rather than exact text. Use simple, directive prompts.
- **[LLM availability]** Acceptance tests require Azure OpenAI access. → Mitigation: Mark tests `[Explicit]` and use `Assert.Inconclusive` when the LLM is unreachable. Tests only run on demand or in environments with LLM access.
- **[MCP SDK client package]** The `ModelContextProtocol` NuGet package must be added to the AcceptanceTests project for `McpClient` and `StdioClientTransport`. → Mitigation: Same package already used by the McpServer project. Approval required per project rules.

## Open Questions

- Should the MCP server binary be included in the Docker image and CI/CD pipeline, or remain a local dev tool only for now?
- Should the Aspire AppHost (`ChurchBulletin.AppHost`) orchestrate the MCP server as an additional resource?

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

## Risks / Trade-offs

- **[NuGet dependency]** Adding the `ModelContextProtocol` package is a new dependency. → Mitigation: It is the official MCP C# SDK and is MIT-licensed. Approval required per project rules.
- **[Database connection]** The MCP server needs a database connection string for EF Core. → Mitigation: Use the same `appsettings.json` / environment variable pattern as the UI server. For local dev, point to LocalDB.
- **[Process lifecycle]** Stdio MCP servers are started/stopped by the AI client. → Mitigation: Standard .NET generic host handles graceful shutdown. No long-running background tasks needed.
- **[State command complexity]** State commands have preconditions (valid transitions, authorization). → Mitigation: Reuse existing `IStateCommand.IsValid()` and status validation. Return clear error messages in MCP tool responses when preconditions fail.

## Open Questions

- Should the MCP server binary be included in the Docker image and CI/CD pipeline, or remain a local dev tool only for now?
- Should the Aspire AppHost (`ChurchBulletin.AppHost`) orchestrate the MCP server as an additional resource?

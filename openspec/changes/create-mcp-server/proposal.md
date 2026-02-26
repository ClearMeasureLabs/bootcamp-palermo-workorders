## Why

The ChurchBulletin system currently exposes its domain operations only through a Blazor UI and Web API. AI agents and LLM-powered clients have no standardized way to interact with work orders, employees, or other domain entities. The Model Context Protocol (MCP) is an emerging open standard for connecting AI assistants to external tools and data sources. Adding an MCP server enables any MCP-compatible AI client (Claude, Copilot, Cursor, etc.) to query and manage work orders programmatically, unlocking AI-assisted workflows without modifying the existing UI or API.

## What Changes

- New .NET project (`src/McpServer/`) hosting an MCP server that exposes ChurchBulletin domain operations as MCP tools and resources
- MCP tools for work order operations: list, query by number, create, update status
- MCP tools for employee operations: list, query by username
- MCP resources for read-only reference data (work order statuses, roles)
- Reuse of existing MediatR queries and `IBus` abstraction — no duplicate business logic
- Integration into the solution file (`ChurchBulletin.sln`)
- Integration tests covering MCP tool invocations
- Acceptance tests that launch the MCP server, connect an MCP client, and validate tools through LLM prompts

## Capabilities

### New Capabilities
- `mcp-server`: MCP protocol server hosting, tool registration, and transport handling (stdio and/or SSE)
- `mcp-work-order-tools`: MCP tools exposing work order query and mutation operations
- `mcp-employee-tools`: MCP tools exposing employee query operations
- `mcp-reference-resources`: MCP resources providing read-only reference data (statuses, roles)
- `mcp-acceptance-tests`: Full system acceptance tests that start the MCP server, connect via MCP client SDK, wire tools into an IChatClient (LLM), and validate end-to-end behavior through natural-language prompts

### Modified Capabilities
<!-- No existing spec-level behavior changes. The MCP server is additive and sits in the outer layer. -->

## Impact

- **New project**: `src/McpServer/` added to the solution, referencing Core and DataAccess (outer layer in Onion Architecture)
- **Dependencies**: Will require an MCP SDK NuGet package (e.g., `ModelContextProtocol` or `Microsoft.Extensions.AI` MCP support)
- **DI/Hosting**: New Lamar service registry or standard .NET DI for the MCP server host
- **Database**: No schema changes — reuses existing EF Core DataContext
- **CI/CD**: Pipeline may need a new build/test target for the McpServer project
- **Deployment**: Can run as a standalone process or be integrated into the existing server host

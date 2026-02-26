## ADDED Requirements

### Requirement: MCP server project exists in the solution
The system SHALL include a new project `src/McpServer/` in the `ChurchBulletin.sln` solution that hosts an MCP-compliant server. The project SHALL target .NET 10.0 and reference Core and DataAccess projects only (Onion Architecture outer layer).

#### Scenario: Project builds successfully
- **WHEN** `dotnet build src/ChurchBulletin.sln` is executed
- **THEN** the McpServer project compiles without errors

#### Scenario: Project references follow Onion Architecture
- **WHEN** the McpServer project references are inspected
- **THEN** it references Core and DataAccess projects
- **AND** it does not reference UI.Server, UI.Client, UI.Api, or LlmGateway

### Requirement: MCP server uses stdio transport
The system SHALL host the MCP server using stdio transport, enabling AI clients (Claude Code, Cursor, etc.) to launch and communicate with it as a subprocess.

#### Scenario: Server starts and responds to MCP initialize request
- **WHEN** the McpServer process is started via stdio
- **AND** an MCP `initialize` request is sent
- **THEN** the server responds with its capabilities including supported tools and resources

#### Scenario: Server shuts down gracefully
- **WHEN** the AI client closes the stdin pipe
- **THEN** the McpServer process exits cleanly without errors

### Requirement: MCP server registers domain tools and resources via DI
The system SHALL use `Microsoft.Extensions.Hosting` and register MediatR handlers, `IBus`, and EF Core `DataContext` using the same DI patterns as the UI server. All MCP tools and resources SHALL be registered during host startup.

#### Scenario: IBus is available to tool handlers
- **WHEN** an MCP tool is invoked
- **THEN** the tool handler resolves `IBus` from DI and executes the corresponding MediatR query or command

#### Scenario: DataContext connects to the configured database
- **WHEN** the MCP server starts with a valid connection string
- **THEN** EF Core `DataContext` connects to the database successfully

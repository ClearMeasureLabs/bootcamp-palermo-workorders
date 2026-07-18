# Plan: Embed MCP HTTP Server into UI.Server at /mcp

## Goal
Host the MCP HTTP transport inside the existing UI.Server process at route `/mcp`, eliminating the need for a separate MCP server process in the Docker container. The standalone McpServer project remains for stdio transport use cases.

## Steps

### 1. Add project reference and NuGet packages to UI.Server
**File:** `src/UI/Server/UI.Server.csproj`
- Add project reference to `..\..\McpServer\McpServer.csproj`
- Add NuGet packages: `ModelContextProtocol` (1.0.0), `ModelContextProtocol.AspNetCore` (1.0.0)

### 2. Register MCP services and map /mcp route in UI.Server Program.cs
**File:** `src/UI/Server/Program.cs`
- Add `using` statements for `ClearMeasure.Bootcamp.McpServer.Tools` and `ClearMeasure.Bootcamp.McpServer.Resources`
- After existing service registration (after `AddApplicationInsightsTelemetry`), register MCP:
  ```csharp
  builder.Services
      .AddMcpServer(options =>
      {
          options.ServerInfo = new() { Name = "ChurchBulletin", Version = "1.0.0" };
      })
      .WithHttpTransport()
      .WithTools<WorkOrderTools>()
      .WithTools<EmployeeTools>()
      .WithResources<ReferenceResources>();
  ```
- Map the MCP endpoint before `MapFallbackToFile` (so `/mcp` doesn't get caught by the Blazor fallback):
  ```csharp
  app.MapMcp();
  ```
  `MapMcp()` maps to `/mcp` by default — exactly the desired route.

### 3. Update McpHttpServerFixture to connect to UI.Server instead of a separate process
**File:** `src/AcceptanceTests/McpServer/McpHttpServerFixture.cs`

The fixture currently launches a separate MCP server process on `http://localhost:3001`. Change it to:
- Remove the separate process launch (build step, `dotnet run --http`, process lifecycle)
- Remove `_serverProcess` field and kill logic
- Instead, depend on `ServerFixture` having already started UI.Server
- Point the `HttpClientTransport` `Endpoint` at `ServerFixture.ApplicationBaseUrl + "/mcp"` (i.e. `https://localhost:7174/mcp`)
- Use `DangerousAcceptAnyServerCertificateValidator` on the HTTP client (UI.Server uses HTTPS)
- Keep SQLite WAL mode setup (still needed for concurrent access)

### 4. Build, run unit tests, run integration tests, run acceptance tests
- Run private build to verify compilation and unit/integration tests
- Run all acceptance tests to verify MCP HTTP tests connect through UI.Server's `/mcp`

### 5. Commit and push

## What Does NOT Change
- `McpServer/Program.cs` — standalone server untouched, still supports stdio and HTTP
- `McpServiceRegistry.cs` — DI registration is transport-agnostic
- Tools, Resources — transport-agnostic
- Existing stdio acceptance tests — continue working unchanged
- Core, DataAccess — Onion Architecture preserved
- Dockerfile — already packages only UI.Server; McpServer comes along as a project reference
- GitHub workflow — no changes needed

## Key Architectural Notes
- UI.Server already has `IDistributedBus` registered as `DistributedBus` (real NServiceBus). The MCP tools will get this real bus from DI, which is an improvement over the standalone MCP server's `NullDistributedBus`.
- The McpServer project reference brings in the tool/resource classes. The `McpServiceRegistry` is NOT registered in UI.Server — `UiServiceRegistry` already handles all the shared services (DbContext, MediatR, IBus). Only the MCP-specific services (`AddMcpServer`) are added.

# Plan: Deploy MCP Server as Remote HTTP Server in Existing Docker Container

## Current State

The MCP server (`src/McpServer/`) exists as a standalone console app using **stdio transport** (`WithStdioServerTransport()`). It runs as a separate `Host.CreateApplicationBuilder` process — local-only. The Docker container only runs `UI.Server`.

## Goal

Package the MCP server into the existing `UI.Server` ASP.NET host so it runs inside the same Docker container and is accessible remotely over HTTP (Streamable HTTP / SSE).

## Implementation Steps

### Step 1: Add `ModelContextProtocol.AspNetCore` NuGet package to `UI.Server`

Add the `ModelContextProtocol.AspNetCore` package to `src/UI/Server/UI.Server.csproj`. This package provides `WithHttpTransport()` and `MapMcp()` extension methods needed for HTTP-based MCP serving.

Also add a project reference from `UI.Server` to the `McpServer` project (to reuse the existing tool and resource classes).

**Files changed:**
- `src/UI/Server/UI.Server.csproj` — add PackageReference for `ModelContextProtocol.AspNetCore` and ProjectReference to McpServer

### Step 2: Register MCP services in `UI.Server/Program.cs`

Add MCP server registration to the existing `UI.Server` `Program.cs`:
- Call `builder.Services.AddMcpServer()` with server info configuration
- Use `.WithHttpTransport()` instead of `.WithStdioServerTransport()`
- Register `.WithTools<WorkOrderTools>()`, `.WithTools<EmployeeTools>()`, `.WithResources<ReferenceResources>()`
- Add `app.MapMcp()` to expose the `/mcp` endpoint (Streamable HTTP) alongside the existing Blazor/API routes

The MCP tools use `IBus` (MediatR) for all domain operations, which is already registered in the UI.Server DI container via `UiServiceRegistry`. No duplicate business logic needed.

**Files changed:**
- `src/UI/Server/Program.cs` — add MCP server registration and endpoint mapping

### Step 3: Update the Docker container to expose the MCP endpoint

The existing `Dockerfile` already runs `UI.Server` on ports 8080/80. Since MCP is served from the same ASP.NET host, no Dockerfile changes are needed — the MCP endpoints (`/mcp`) will be available on the same ports.

The Azure Container App ingress is already configured to route HTTPS traffic to the container. The MCP endpoint will be accessible at `https://<container-app-fqdn>/mcp`.

**Files changed:**
- None (Dockerfile and Azure infra unchanged)

### Step 4: Keep `McpServer` project for local stdio usage

The standalone `McpServer/Program.cs` with stdio transport remains available for local developer use (e.g., connecting from Claude Desktop or VS Code). No changes to the standalone project.

**Files changed:**
- None

### Step 5: Add unit/integration tests for HTTP MCP endpoint

Add a test that verifies the MCP server responds to HTTP requests when hosted within UI.Server. The test should use the `ModelContextProtocol` client SDK with `SseClientTransport` to connect to the remotely-hosted MCP endpoint and invoke a tool.

**Files changed:**
- `src/IntegrationTests/McpServer/` or `src/AcceptanceTests/McpServer/` — add remote MCP tests

### Step 6: Update documentation

Update the openspec design doc and any relevant documentation to reflect that the MCP server is now available both locally (stdio) and remotely (HTTP via UI.Server container).

**Files changed:**
- `openspec/changes/create-mcp-server/design.md`

## Architecture Summary

```
Azure Container App
└── Docker Container (UI.Server)
    ├── Blazor WASM UI          → /
    ├── API Controllers         → /api/*
    ├── Health Check            → /_healthcheck
    └── MCP Server (NEW)        → /mcp (Streamable HTTP + SSE)
        ├── WorkOrderTools
        ├── EmployeeTools
        └── ReferenceResources
```

The MCP endpoint shares the same database connection, DI container, and authentication context as the rest of UI.Server. Remote MCP clients connect via:
```
https://<container-app-fqdn>/mcp
```

## Key Decisions

1. **Same process, not a sidecar** — The MCP server runs inside UI.Server's ASP.NET pipeline, not as a separate container or process. This keeps deployment simple and shares the existing DI/database infrastructure.

2. **`ModelContextProtocol.AspNetCore`** — This package provides the HTTP transport layer. It maps both Streamable HTTP (2025-03-26 spec) and legacy SSE endpoints for backward compatibility.

3. **No Dockerfile changes** — Since MCP is just another set of HTTP endpoints in the same app, no container configuration changes are needed.

4. **Standalone stdio project preserved** — Developers can still use `McpServer` locally for stdio-based tools like Claude Desktop.

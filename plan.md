# Plan: Add HTTP Transport to MCP Server

## Summary

Add Streamable HTTP transport (with SSE backward compatibility) to the existing MCP server, which currently supports only stdio transport. This enables remote AI agents and HTTP-based MCP clients to connect to the server over the network, in addition to the existing local stdio mode.

## Current State

- **McpServer project** (`src/McpServer/`): Standalone console app using `Host.CreateApplicationBuilder`, registered with `WithStdioServerTransport()`, exposes 7 work order tools + 2 employee tools + 3 resources
- **NuGet**: `ModelContextProtocol` 1.0.0-rc.1
- **Transport**: stdio only (child process model)
- **Acceptance tests**: Use `StdioClientTransport` to launch server as subprocess

## Changes

### Step 1: Update NuGet packages

**File: `src/McpServer/McpServer.csproj`**

- Upgrade `ModelContextProtocol` from `1.0.0-rc.1` to `1.0.0`
- Add `ModelContextProtocol.AspNetCore` version `1.0.0` (provides `WithHttpTransport()` and `MapMcp()`)
- Add `Microsoft.AspNetCore.App` framework reference (required for ASP.NET Core APIs)

**File: `src/AcceptanceTests/AcceptanceTests.csproj`**

- Upgrade `ModelContextProtocol` from `1.0.0-rc.1` to `1.0.0`

### Step 2: Update McpServer project to support both transports

**File: `src/McpServer/McpServer.csproj`**

- Change SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web` to get ASP.NET Core hosting
- Remove explicit `Microsoft.Extensions.Hosting` package reference (included in Web SDK)

**File: `src/McpServer/Program.cs`**

Convert from generic host to web application host with transport selection via command-line argument or environment variable:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Logging: send to stderr so stdio transport isn't disrupted
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Host.UseLamar(registry => { registry.IncludeRegistry<McpServiceRegistry>(); });

var mcpBuilder = builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "ChurchBulletin",
            Version = "1.0.0"
        };
    })
    .WithTools<WorkOrderTools>()
    .WithTools<EmployeeTools>()
    .WithResources<ReferenceResources>();

var useHttp = args.Contains("--http") ||
    string.Equals(builder.Configuration["Transport"], "http", StringComparison.OrdinalIgnoreCase);

if (useHttp)
{
    mcpBuilder.WithHttpTransport();
}
else
{
    mcpBuilder.WithStdioServerTransport();
}

var app = builder.Build();

if (useHttp)
{
    app.MapMcp();
}

await app.RunAsync();
```

**Key design decisions:**

- **Transport selection via `--http` flag or `Transport` config**: Defaults to stdio (preserving backward compatibility). Pass `--http` or set `Transport=http` in config/env to enable HTTP.
- **`WebApplication.CreateBuilder`**: Required by `ModelContextProtocol.AspNetCore` for `MapMcp()`. Works for both transports — stdio transport still functions under a web host.
- **No port hardcoding**: Use standard ASP.NET Core URL configuration (`--urls`, `ASPNETCORE_URLS`, `appsettings.json`). Default will be `http://localhost:5000`.

### Step 3: Add HTTP configuration to appsettings

**File: `src/McpServer/appsettings.json`**

Add a Kestrel URL configuration for when HTTP mode is used:

```json
{
  "ConnectionStrings": {
    "SqlConnectionString": "Server=(LocalDb)\\MSSQLLocalDB;Database=ChurchBulletin;Integrated Security=true;MultipleActiveResultSets=true"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:3001"
      }
    }
  }
}
```

### Step 4: Add HTTP acceptance tests

**File: `src/AcceptanceTests/McpServer/McpHttpServerFixture.cs`** (new)

A parallel `[SetUpFixture]` that starts the MCP server in HTTP mode and connects via `SseClientTransport` (or `HttpClientTransport`):

```csharp
[SetUpFixture]
public class McpHttpServerFixture
{
    private static Process? _serverProcess;
    public static McpClient? McpClientInstance { get; private set; }
    public static IList<McpClientTool>? Tools { get; private set; }
    public static bool ServerAvailable { get; private set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Reuse database initialization from ServerFixture
        ServerFixture.InitializeDatabaseOnce();

        var connectionString = ResolveConnectionString();
        await StartMcpHttpServer(connectionString);
    }

    private static async Task StartMcpHttpServer(string connectionString)
    {
        // Build and start the server with --http flag
        // Use SseClientTransport to connect to http://localhost:3001/sse
        // Discover tools and verify connectivity
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Dispose client, kill server process
    }
}
```

**File: `src/AcceptanceTests/McpServer/McpHttpServerAcceptanceTests.cs`** (new)

Mirror the existing `McpServerAcceptanceTests` but using the HTTP transport fixture:

- `ShouldDiscoverAllMcpToolsViaHttp` — connects over HTTP and discovers tools
- `ShouldCreateWorkOrderViaHttpDirectToolCall` — creates a work order via HTTP MCP
- `ShouldListWorkOrdersViaHttp` — lists work orders over HTTP

### Step 5: Update Aspire AppHost (optional)

**File: `src/ChurchBulletin.AppHost/AppHost.cs`**

Register the MCP server as an additional project in the Aspire orchestration with HTTP transport:

```csharp
builder.AddProject<Projects.McpServer>("mcp-server")
    .WithReference(sql)
    .WithArgs("--http");
```

## Files Changed

| File | Action |
|------|--------|
| `src/McpServer/McpServer.csproj` | Modify — change SDK, update packages, add AspNetCore |
| `src/McpServer/Program.cs` | Modify — WebApplication host, transport selection |
| `src/McpServer/appsettings.json` | Modify — add Kestrel endpoint config |
| `src/AcceptanceTests/AcceptanceTests.csproj` | Modify — upgrade ModelContextProtocol |
| `src/AcceptanceTests/McpServer/McpHttpServerFixture.cs` | New — HTTP fixture |
| `src/AcceptanceTests/McpServer/McpHttpServerAcceptanceTests.cs` | New — HTTP tests |

## What Does NOT Change

- `McpServiceRegistry.cs` — DI registration is transport-agnostic
- `DatabaseConfiguration.cs` — no transport dependency
- `NullDistributedBus.cs` — no change
- `Tools/WorkOrderTools.cs` — tools are transport-agnostic
- `Tools/EmployeeTools.cs` — tools are transport-agnostic
- `Resources/ReferenceResources.cs` — resources are transport-agnostic
- Existing stdio acceptance tests — continue working unchanged
- Core, DataAccess projects — no changes (Onion Architecture preserved)

## Risks

- **SDK version upgrade** (`1.0.0-rc.1` → `1.0.0`): The 1.0.0 stable release was published today (2026-02-25). API surface should be compatible with rc.1 but may have minor breaking changes.
- **WebApplication vs Host**: Switching from `Host.CreateApplicationBuilder` to `WebApplication.CreateBuilder` changes the host, but `WithStdioServerTransport()` should still function correctly under a web host since stdio transport doesn't use Kestrel.
- **No authentication**: HTTP mode exposes the MCP server on the network without auth. This is acceptable for local development; production deployment would need auth added later.

# Work Order Management - C4 Architecture

PlantUML C4 diagrams for the current Work Order portal (Onion Architecture, .NET 10, Blazor WASM + Server).

| File | Diagram type | Description |
|------|--------------|-------------|
| `arch-c4-work-orders-context.puml` | C4Context | People, the portal system, and external systems (SQL, OpenAI, App Insights, ACR) |
| `arch-c4-work-orders-container.puml` | C4Container | Runtime containers: UI.Client, UI.Server, Worker, McpServer, SQL, NServiceBus, CI/CD |

## Request flow (summary)

```
Staff browser -> UI.Client (Blazor WASM)
  -> UI.Server (HTTPS / HTTP2)
    -> UI.Api / gRPC WorkOrders / MCP /mcp
    -> IBus (MediatR) -> DataAccess handlers -> Azure SQL
    -> NServiceBus SqlServerTransport <-> Worker
    -> LlmGateway -> Azure OpenAI (optional)
    -> OpenTelemetry -> Application Insights (optional)
```

## Rendering

```bash
./arch/render-diagrams.sh
# or
pwsh arch/render-diagrams.ps1
```

Sources use the repo PlantUML theme (`templates/plantuml-theme.puml`) and pin C4-PlantUML to `release/1-0`.

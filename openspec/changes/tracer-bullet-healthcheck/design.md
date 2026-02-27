## Context

The application uses ASP.NET Core's built-in health check framework (`Microsoft.Extensions.Diagnostics.HealthChecks`). Health checks implement `IHealthCheck` and are registered via `AddHealthChecks().AddCheck<T>()` in `UIServiceRegistry`. Routes are mapped with `MapHealthChecks()` and support tag-based filtering via `HealthCheckOptions.Predicate`.

Current health check routing:

```
/health          → all checks (Aspire default via MapDefaultEndpoints)
/alive           → only checks tagged "live"
/_healthcheck    → all checks (no filter)
```

Current registered checks:
- `CanConnectToDatabaseHealthCheck` ("DataAccess") — verifies DB connectivity
- `CanConnectToLlmServerHealthCheck` ("LlmGateway") — verifies LLM reachability
- `Is64BitProcessHealthCheck` ("Server") — verifies 64-bit process
- `HealthCheck` ("API") — always healthy
- `FunJeffreyCustomEventHealthCheck` ("Jeffrey") — custom event check

The domain uses a CQRS pattern: state commands (`SaveDraftCommand`, etc.) implement `IStateCommand` and are dispatched through `IBus` (wrapping MediatR). The `StateCommandHandler` handles persistence via EF Core `DbContext` and publishes events via `IDistributedBus`.

## Goals / Non-Goals

**Goals:**
- Add a `/_tracerbullethealthcheck` route that runs only tracer-bullet-tagged health checks
- Ensure existing `/_healthcheck` excludes tracer bullet checks so monitoring is unaffected
- Implement one tracer bullet that exercises the full work order creation lifecycle through the real `IBus`/MediatR pipeline
- Clean up phantom data (employee + work order) via explicit delete in a separate transaction
- Follow existing health check patterns (constructor injection, `IHealthCheck` interface)

**Non-Goals:**
- Wrapping the entire operation in a rolled-back transaction (explicit delete is preferred)
- Adding tracer bullets for every domain operation (start with one, expand later)
- Adding authentication to the tracer bullet endpoint
- Modifying Core or adding new domain types for tracer bullet support

## Decisions

### Decision 1: Tag-based routing using `"tracerbullet"` tag

**Rationale:** ASP.NET Core health checks already support tags and predicate-based filtering. The `/alive` endpoint demonstrates this pattern with the `"live"` tag. Using a `"tracerbullet"` tag for the new checks keeps the mechanism consistent and requires no custom infrastructure.

**Route mapping:**
```
/_tracerbullethealthcheck  →  Predicate: tags.Contains("tracerbullet")
/_healthcheck              →  Predicate: !tags.Contains("tracerbullet")
/health                    →  Predicate: !tags.Contains("tracerbullet")
/alive                     →  Predicate: tags.Contains("live") (unchanged)
```

**Alternatives considered:**
- Separate health check service registration: Would require a second `AddHealthChecks()` call with a different name. More complex, less idiomatic.
- Custom middleware: Over-engineered for what tags already solve.

### Decision 2: Tracer bullet class lives in `DataAccess` project

**Rationale:** The tracer bullet needs `DbContext` for direct entity creation/deletion and `IBus` for MediatR command dispatch. `DataAccess` already references Core and houses `CanConnectToDatabaseHealthCheck`. The new class follows the same pattern — a health check that depends on data access infrastructure.

**Alternatives considered:**
- `UI.Server`: Would work (outer layer can reference everything) but mixes infrastructure health checks with application-level verification. DataAccess is more cohesive.
- New project: Unnecessary for a single class. Can revisit if tracer bullets grow into a large suite.

### Decision 3: Execute through `IBus` (MediatR pipeline), not direct DbContext

**Rationale:** The purpose of a tracer bullet is to prove the real code path works. The `SaveDraftCommand` flows through `StateCommandHandler`, which exercises: MediatR dispatch, `TimeProvider`, EF Core attach/add/save, and `IDistributedBus` event publishing. Direct DbContext operations would skip all of this.

The tracer bullet will:
1. Create an `Employee` directly via `DbContext` (no MediatR command exists for employee creation)
2. Execute `SaveDraftCommand` via `IBus` (exercises the full handler pipeline)
3. Verify the work order via `DbContext.Find` (read-back verification)
4. Delete work order and employee via `DbContext.Remove` + `SaveChanges` (cleanup)

### Decision 4: Explicit delete cleanup (not transaction rollback)

**Rationale:** The user specified that cleanup should use a new database transaction for deletion rather than rolling back the creation transaction. This approach:
- Proves the write actually commits to the database (not just buffered)
- Verifies the read-back works against committed data
- Tests the delete path as well

The cleanup follows a try/finally pattern: the check attempts to delete phantom data even if the verification step fails. A recognizable sentinel in the employee username (`_tracerbullet_{guid}`) makes any leftover data identifiable.

### Decision 5: Timeout-aware execution

**Rationale:** Tracer bullets inherently take longer than connectivity checks. ASP.NET Core health checks support a `timeout` parameter per registration. The tracer bullet should be registered with a reasonable timeout (e.g., 30 seconds) so a slow database doesn't cause the check to hang indefinitely. The `/_tracerbullethealthcheck` route should also configure `HealthCheckOptions.Timeout` if needed.

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│ HTTP Request: GET /_tracerbullethealthcheck                      │
└───────────────────────┬──────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────────┐
│ ASP.NET Health Check Middleware                                   │
│ Predicate: r.Tags.Contains("tracerbullet")                       │
└───────────────────────┬──────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────────┐
│ WorkOrderLifecycleTracerBullet : IHealthCheck                    │
│ Tag: "tracerbullet"                                              │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─── SETUP ──────────────────────────────────────────────────┐  │
│  │ 1. Generate unique username: "_tracerbullet_{guid}"        │  │
│  │ 2. Create Employee entity                                  │  │
│  │ 3. DbContext.Add(employee) + SaveChanges                   │  │
│  └────────────────────────────────────────────────────────────┘  │
│                         │                                        │
│                         ▼                                        │
│  ┌─── EXECUTE ────────────────────────────────────────────────┐  │
│  │ 4. Create WorkOrder (Creator = employee, Id = Empty)       │  │
│  │ 5. IBus.Send(SaveDraftCommand(workOrder, employee))        │  │
│  │    └─► MediatR → StateCommandHandler                       │  │
│  │        └─► EF Core Attach/Add/Save                         │  │
│  │        └─► IDistributedBus.PublishAsync (event)            │  │
│  └────────────────────────────────────────────────────────────┘  │
│                         │                                        │
│                         ▼                                        │
│  ┌─── VERIFY ─────────────────────────────────────────────────┐  │
│  │ 6. DbContext.Find<WorkOrder>(workOrder.Id)                 │  │
│  │ 7. Assert: found, Title matches, Status == Draft           │  │
│  └────────────────────────────────────────────────────────────┘  │
│                         │                                        │
│                         ▼                                        │
│  ┌─── CLEANUP (try/finally) ──────────────────────────────────┐  │
│  │ 8. DbContext.Remove(workOrder) + SaveChanges               │  │
│  │ 9. DbContext.Remove(employee) + SaveChanges                │  │
│  └────────────────────────────────────────────────────────────┘  │
│                         │                                        │
│                         ▼                                        │
│  Success: HealthCheckResult.Healthy("Work order lifecycle OK")   │
│  Failure: HealthCheckResult.Unhealthy(exception.Message)         │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

## Dependency Flow

```
WorkOrderLifecycleTracerBullet (DataAccess)
    ├── DbContext           (injected — employee create/delete, work order verify/delete)
    ├── IBus                (injected — sends SaveDraftCommand through MediatR)
    ├── IWorkOrderNumberGenerator (injected — generates work order number)
    └── ILogger             (injected — diagnostic logging)
```

All dependencies already exist and are registered in the DI container. No new interfaces or abstractions needed.

## Route Configuration Changes

**Program.cs** — before:
```csharp
app.MapHealthChecks("_healthcheck");
```

**Program.cs** — after:
```csharp
app.MapHealthChecks("_healthcheck", new HealthCheckOptions
{
    Predicate = r => !r.Tags.Contains("tracerbullet")
});

app.MapHealthChecks("_tracerbullethealthcheck", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("tracerbullet")
});
```

**Extensions.cs** `MapDefaultEndpoints` — update `/health` to also exclude tracer bullets:
```csharp
app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
{
    Predicate = r => !r.Tags.Contains("tracerbullet")
});
```

**UIServiceRegistry.cs** — add the tracer bullet registration:
```csharp
this.AddHealthChecks()
    .AddCheck<CanConnectToLlmServerHealthCheck>("LlmGateway")
    .AddCheck<CanConnectToDatabaseHealthCheck>("DataAccess")
    .AddCheck<Is64BitProcessHealthCheck>("Server")
    .AddCheck<HealthCheck>("API")
    .AddCheck<FunJeffreyCustomEventHealthCheck>("Jeffrey")
    .AddCheck<WorkOrderLifecycleTracerBullet>(
        "TracerBullet-WorkOrderLifecycle",
        tags: ["tracerbullet"],
        timeout: TimeSpan.FromSeconds(30));
```

## Risks / Trade-offs

- **[Phantom data on failure]** If the check fails after creating data but before cleanup, orphaned records remain. → Mitigation: try/finally ensures cleanup runs even on verification failure. Sentinel username prefix (`_tracerbullet_`) makes orphans identifiable. A periodic cleanup query could remove old tracer bullet data if needed.
- **[IDistributedBus side effects]** The `SaveDraftCommand` flows through `StateCommandHandler` which calls `IDistributedBus.PublishAsync`. In production this publishes to NServiceBus. → Mitigation: The event payload contains the tracer bullet work order data. Downstream handlers should be idempotent. The work order is deleted shortly after, so any handler processing the event will find no data. If this becomes a concern, a `NoOpDistributedBus` could be injected for tracer bullet scope, but this is not needed initially.
- **[Performance under load]** The tracer bullet executes real DB writes. Under heavy load, concurrent tracer bullet checks could contend. → Mitigation: The endpoint is called on-demand, not polled frequently. A single concurrent execution is expected. Could add a semaphore if needed later.
- **[DbContext lifetime]** The health check receives a scoped `DbContext`. The tracer bullet creates entities, then sends a command through `IBus` which resolves its own `DbContext` in the handler. The verification read-back needs a fresh `DbContext` to avoid reading cached data. → Mitigation: Use `IServiceScopeFactory` to create fresh scopes for the verify and cleanup phases, ensuring real database round-trips.

## Open Questions

- Should the tracer bullet endpoint require any form of authorization or rate limiting to prevent abuse in production?
- Should the CI/CD deploy pipeline add a `/_tracerbullethealthcheck` call after the existing `/_healthcheck` smoke test?

## Overview

A health check class that performs a phantom work order lifecycle — creating a test employee, saving a draft work order through the full MediatR pipeline, verifying persistence, then cleaning up both entities — to prove the system's core business operation works end-to-end.

## Requirements

### REQ-1: Implement IHealthCheck

`WorkOrderLifecycleTracerBullet` implements `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck`. It lives in the `ClearMeasure.Bootcamp.DataAccess` namespace in the `DataAccess` project.

### REQ-2: Constructor dependencies

Injected via constructor:
- `IServiceScopeFactory` — creates fresh DI scopes for each phase to avoid DbContext caching
- `ILogger<WorkOrderLifecycleTracerBullet>` — diagnostic logging

### REQ-3: Setup phase — create test employee

1. Create a new DI scope
2. Resolve `DbContext` from the scope
3. Create an `Employee` with:
   - `Id` = `Guid.NewGuid()`
   - `UserName` = `"_tracerbullet_{newGuid}"` (unique per execution, identifiable as phantom data)
   - `FirstName` = `"Tracer"`
   - `LastName` = `"Bullet"`
   - `EmailAddress` = `"tracerbullet@test.local"`
4. `DbContext.Add(employee)` then `SaveChangesAsync`
5. Dispose the scope

### REQ-4: Execute phase — save draft work order through IBus

1. Create a new DI scope
2. Resolve `IBus` and `IWorkOrderNumberGenerator` from the scope
3. Create a `WorkOrder` with:
   - `Id` = `Guid.Empty` (signals new entity to `StateCommandHandler`)
   - `Title` = `"Tracer Bullet Test"`
   - `Description` = `"Automated tracer bullet health check"`
   - `Creator` = the employee from SETUP
   - `Number` = generated via `IWorkOrderNumberGenerator.GenerateNumber()`
   - `CreatedDate` = `null` (will be set by `SaveDraftCommand.Execute`)
4. Construct `SaveDraftCommand(workOrder, employee)`
5. Send via `IBus.Send(command)` — this exercises the full pipeline:
   - MediatR dispatch
   - `StateCommandHandler.Handle`
   - `StateCommandContext` with `TimeProvider`
   - EF Core attach/add/save
   - `IDistributedBus.PublishAsync`
6. Capture the returned `StateCommandResult` — extract the persisted work order (with its assigned `Id`)
7. Dispose the scope

### REQ-5: Verify phase — confirm persistence

1. Create a new DI scope
2. Resolve `DbContext` from the scope
3. `DbContext.Find<WorkOrder>(workOrderId)` using the Id from the execute phase
4. Verify the work order is not null
5. Verify `Title` matches `"Tracer Bullet Test"`
6. Verify `Status` equals `WorkOrderStatus.Draft`
7. Dispose the scope
8. If any verification fails, throw an exception (caught by the outer try/catch to return Unhealthy)

### REQ-6: Cleanup phase — delete phantom data

1. Cleanup executes in a `finally` block so it runs even if verify fails
2. Create a new DI scope
3. Resolve `DbContext` from the scope
4. If the work order was created (Id != Empty):
   - Attach and remove the work order: `DbContext.Remove(workOrder)` then `SaveChangesAsync`
5. Attach and remove the employee: `DbContext.Remove(employee)` then `SaveChangesAsync`
6. Dispose the scope
7. Log any cleanup failures but do not throw — the primary result (Healthy/Unhealthy) should reflect the business operation outcome, not cleanup issues

### REQ-7: Return values

- **All phases succeed**: `HealthCheckResult.Healthy("Work order lifecycle tracer bullet passed")`
- **Any phase fails** (exception): `HealthCheckResult.Unhealthy("Work order lifecycle tracer bullet failed: {exception.Message}", exception)`
- Log the full exception at `LogError` level on failure
- Log a summary at `LogInformation` level on success

### REQ-8: Registration with tag

Register in `UIServiceRegistry` with:
- Name: `"TracerBullet-WorkOrderLifecycle"`
- Tags: `["tracerbullet"]`
- Timeout: `TimeSpan.FromSeconds(30)`

### REQ-9: Route configuration

**New route:**
```
/_tracerbullethealthcheck → only checks tagged "tracerbullet"
```

**Modified route:**
```
/_healthcheck → exclude checks tagged "tracerbullet"
```

**Modified Aspire default route:**
```
/health → exclude checks tagged "tracerbullet"
```

### REQ-10: CancellationToken respect

The `CheckHealthAsync` method receives a `CancellationToken`. Pass it through to all `SaveChangesAsync` and `IBus.Send` calls so the check can be cancelled by the health check timeout.

## Acceptance Criteria

1. `GET /_tracerbullethealthcheck` returns `Healthy` when the database is accessible and the MediatR pipeline is correctly wired
2. `GET /_tracerbullethealthcheck` returns `Unhealthy` with a descriptive message when any phase fails
3. `GET /_healthcheck` does NOT execute the tracer bullet check (response time unaffected)
4. `GET /health` does NOT execute the tracer bullet check
5. No phantom employee or work order data remains in the database after a successful check
6. The tracer bullet employee username starts with `_tracerbullet_` for identification of any orphaned data

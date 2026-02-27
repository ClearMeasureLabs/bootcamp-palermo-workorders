## Why

The application currently exposes a `/_healthcheck` endpoint that verifies infrastructure dependencies are reachable (database connectivity, LLM gateway, process bitness). These checks answer: "Are the dependencies up?" They do not answer: "Can the system actually perform its core business operations end-to-end?"

Tracer bullet tests fill this gap. Each tracer bullet executes a phantom transaction — a full create-verify-cleanup cycle through the real code paths — and reports Healthy/Unhealthy. They prove the entire vertical slice works: DI wiring, MediatR pipeline, EF Core persistence, database schema, and domain logic. Because they exercise real transactions (not mocked), they catch configuration drift, missing migrations, broken DI registrations, and subtle runtime failures that connectivity checks miss.

A new HTTP route (`/_tracerbullethealthcheck`) runs only tracer bullet health checks, keeping them separate from the fast infrastructure checks that monitoring tools poll frequently.

## What Changes

- New `/_tracerbullethealthcheck` HTTP route that runs only health checks tagged `"tracerbullet"`
- Existing `/_healthcheck` route updated to exclude checks tagged `"tracerbullet"` (prevents slowing down existing monitoring)
- One initial tracer bullet health check class: `WorkOrderLifecycleTracerBullet`
- The tracer bullet creates a test employee, saves a draft work order as that employee through the full MediatR/`IBus` pipeline, verifies persistence, then deletes the work order and employee in a separate cleanup transaction

## Capabilities

### New Capabilities
- `work-order-lifecycle-tracer-bullet`: A health check that exercises the full work order creation lifecycle — employee creation, `SaveDraftCommand` execution through `IBus`, persistence verification, and cleanup — reporting Healthy on success and Unhealthy with diagnostics on failure

### Modified Capabilities
- The existing `/_healthcheck` endpoint gains a predicate filter to exclude `"tracerbullet"`-tagged checks (no behavioral change for existing checks — they still run as before)

## Impact

- **New class**: `WorkOrderLifecycleTracerBullet` in `DataAccess/` (needs `DbContext`, `IBus`, `IWorkOrderNumberGenerator`)
- **Modified file**: `Program.cs` — add `/_tracerbullethealthcheck` route mapping with tag filter; update `/_healthcheck` to exclude tracer bullet tag
- **Modified file**: `UIServiceRegistry.cs` — register the new health check with `"tracerbullet"` tag
- **Modified file**: `Extensions.cs` — update `MapDefaultEndpoints` to be aware of the tracer bullet tag exclusion on the standard `/health` route
- **Database**: No schema changes — tracer bullet creates and deletes its own data within the check
- **CI/CD**: The deploy pipeline's health check call (`/_healthcheck`) is unaffected because tracer bullets are excluded from that route. A separate step could optionally call `/_tracerbullethealthcheck` after deployment.
- **No new NuGet packages**: Uses only `Microsoft.Extensions.Diagnostics.HealthChecks` which is already referenced

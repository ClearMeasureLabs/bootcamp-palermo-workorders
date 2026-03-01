# Lab 12: Production Stability - Health Checks & Monitoring

**Curriculum Section:** Sections 06-07 (Operate/Execute & Reporting)
**Estimated Time:** 30 minutes
**Type:** Analyze + Experiment

---

## Objective

Understand the production stability mechanisms built into the application: health checks, OpenTelemetry, and self-diagnostics. Connect these to the curriculum's principles of "Recovery Quickly" and "Prevent All Stability Problems."

---

## Context

The curriculum defines stability as:
- **Knowing the breaking point** — What fails first under load?
- **Quick issue recovery** — Real-time monitoring and self-diagnostics
- **Preventing stability problems** — After achieving stability, protect it

---

## Steps

### Step 1: Start the Application

```powershell
cd src/UI/Server
dotnet run
```

### Step 2: Hit the Health Endpoint

Navigate to `https://localhost:7174/_healthcheck`.

**Expected response:** A health status response (typically `Healthy` with component details).

This endpoint is used by:
- Azure Container Apps for liveness/readiness probes
- Load balancers to route traffic away from unhealthy instances
- Monitoring systems to alert on degradation

### Step 3: Explore the Database Health Check

Open `src/DataAccess/CanConnectToDatabaseHealthCheck.cs`. Study what it checks:

- Attempts a database connection
- Returns `HealthCheckResult.Healthy` or `HealthCheckResult.Unhealthy`
- Includes diagnostic information in the response

### Step 4: Explore Service Defaults

Open `src/ChurchBulletin.ServiceDefaults/Extensions.cs`. Find:

1. **Health check registration** — Look for `AddHealthChecks()` and related setup
2. **OpenTelemetry setup** — Look for telemetry configuration (tracing, metrics, logging)
3. **Resilience configuration** — Look for HTTP resilience policies

### Step 5: Explore Client-Side Health Monitoring

Open these files in `src/UI/Client/HealthChecks/`:
- `ServerHealthCheck.cs` — Checks if the server is reachable from the WASM client
- `RemotableBusHealthCheck.cs` — Checks if the MediatR bus is functioning

Open `src/UI.Shared/Components/HealthCheckLink.razor` — see how health status is surfaced in the UI.

### Step 6: Experiment - Simulate a Failure

**Option A (safe):** Stop the application, then hit the health endpoint from another terminal:

```powershell
Invoke-WebRequest https://localhost:7174/_healthcheck
```

Observe the connection failure — this is what the load balancer would see.

**Option B (in code):** Temporarily modify the database connection string in your local configuration to point to a nonexistent server. Restart and hit the health endpoint. Observe the degraded response.

**Revert any changes.**

### Step 7: Review Monitoring Architecture

Examine the monitoring stack:
- **OpenTelemetry** — Distributed tracing across services
- **Application Insights** — Azure-hosted telemetry (see `Azure.Monitor.OpenTelemetry.AspNetCore` in UI.Server)
- **Health checks** — Automated probes at `/_healthcheck`

### Step 8: Trace the Stability Layers

Map the application's stability mechanisms to the curriculum's framework:

| Stability Principle | Implementation |
|---------------------|----------------|
| Know the breaking point | Health checks test database, bus, external services |
| Quick issue recovery | Health endpoint enables load balancer failover |
| Real-time monitoring | OpenTelemetry + Application Insights |
| Self-diagnostics | `WhatDoIHaveController` exposes DI container state |
| Prevent stability problems | Resilience policies on HTTP clients |

---

## Expected Outcome

- Understanding of the health check system and its role in production stability
- Knowledge of how OpenTelemetry and Application Insights provide observability
- Experience with simulating failures and observing health degradation

---

## Discussion Questions

1. The health endpoint returns structured data, not just "OK." Why is detailed health information important for operations?
2. How does the health check system implement "Recovery Quickly"? What happens when a container reports unhealthy in Azure Container Apps?
3. The curriculum states "After achieving stability: protect it." How do the automated health checks protect stability over time?
4. What metrics would you add for production monitoring beyond health checks? (Request latency, error rates, queue depth, database connection pool usage)
5. The `WhatDoIHaveController` exposes the DI container contents. When is this useful for diagnosing production issues? What security considerations apply?

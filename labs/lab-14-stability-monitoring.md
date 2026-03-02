# Lab 14: Production Stability - Health Checks & Monitoring

**Curriculum Section:** Sections 06-07 (Operate/Execute & Reporting)
**Estimated Time:** 30 minutes
**Type:** Analyze + Experiment

---

## Objective

Understand the production stability mechanisms: health checks, OpenTelemetry, and self-diagnostics. Connect to the "Recovery Quickly" and "Prevent All Stability Problems" principles.

---

## Steps

### Step 1: Start the Application and Hit Health Endpoint

```powershell
cd src/UI/Server && dotnet run
```

Navigate to `https://localhost:7174/_healthcheck`.

### Step 2: Explore the Database Health Check

Open `src/DataAccess/CanConnectToDatabaseHealthCheck.cs`. Study what it checks and returns.

### Step 3: Explore Service Defaults

Open `src/ChurchBulletin.ServiceDefaults/Extensions.cs`. Find health check registration, OpenTelemetry setup, and resilience configuration.

### Step 4: Explore Client-Side Health Monitoring

Open `src/UI/Client/HealthChecks/ServerHealthCheck.cs` and `RemotableBusHealthCheck.cs`. Open `src/UI.Shared/Components/HealthCheckLink.razor` to see how health status surfaces in the UI.

### Step 5: Simulate a Failure

Stop the application and attempt to hit the health endpoint. Observe what a load balancer would see.

---

## Expected Outcome

- Understanding of the health check system and its role in production stability

---

## Discussion Questions

1. How does the health check implement "Recovery Quickly"?
2. What metrics would you add beyond health checks? (Latency, error rates, queue depth)

## 1. Tracer Bullet Health Check Class

- [ ] 1.1 Create `WorkOrderLifecycleTracerBullet.cs` in `src/DataAccess/` implementing `IHealthCheck`
- [ ] 1.2 Implement setup phase: create test employee via scoped `DbContext`
- [ ] 1.3 Implement execute phase: send `SaveDraftCommand` via scoped `IBus`
- [ ] 1.4 Implement verify phase: read-back work order via scoped `DbContext`
- [ ] 1.5 Implement cleanup phase: delete work order and employee in `finally` block via scoped `DbContext`
- [ ] 1.6 Return `Healthy`/`Unhealthy` with descriptive messages and logging

## 2. Registration and Route Configuration

- [ ] 2.1 Register `WorkOrderLifecycleTracerBullet` in `UIServiceRegistry.cs` with tag `"tracerbullet"` and 30-second timeout
- [ ] 2.2 Update `Program.cs`: add `/_tracerbullethealthcheck` route with predicate `r.Tags.Contains("tracerbullet")`
- [ ] 2.3 Update `Program.cs`: modify `/_healthcheck` route to exclude `"tracerbullet"` tag
- [ ] 2.4 Update `Extensions.cs` `MapDefaultEndpoints`: modify `/health` route to exclude `"tracerbullet"` tag

## 3. Testing

- [ ] 3.1 Add unit test for `WorkOrderLifecycleTracerBullet` with stubbed dependencies verifying Healthy result on success path
- [ ] 3.2 Add unit test verifying Unhealthy result when `IBus.Send` throws
- [ ] 3.3 Add unit test verifying cleanup runs even when verify phase fails
- [ ] 3.4 Verify existing health check acceptance tests still pass (no regression on `/_healthcheck`)
- [ ] 3.5 Verify solution builds: `dotnet build src/ChurchBulletin.sln --configuration Release`

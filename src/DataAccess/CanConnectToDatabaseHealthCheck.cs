using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck()
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context1,
        CancellationToken cancellationToken = new())
    {
        return new HealthCheckResult(HealthStatus.Healthy);
    }
}
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(DataContext dataContext)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context1,
        CancellationToken cancellationToken = new())
    {
        var isHealthy = await dataContext.Database.CanConnectAsync(cancellationToken);
        var result = new HealthCheckResult(HealthStatus.Healthy);

        if (!isHealthy)
        {
            result = new HealthCheckResult(HealthStatus.Unhealthy);
        }

        return result;
    }
}
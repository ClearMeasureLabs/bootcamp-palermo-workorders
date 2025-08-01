using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(ILogger<CanConnectToDatabaseHealthCheck> logger, DataContext context)
    : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context1,
        CancellationToken cancellationToken = new())
    {
        if (!context.Database.CanConnect())
            return Task.FromResult(HealthCheckResult.Unhealthy("Cannot connect to database"));

        logger.LogInformation("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
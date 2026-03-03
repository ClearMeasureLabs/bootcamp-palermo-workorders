using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(DbContext context, ILogger<CanConnectToDatabaseHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context1,
        CancellationToken cancellationToken = new())
    {
        try
        {
            await context.Database.CanConnectAsync(cancellationToken);
            logger.LogDebug("Health check success via DbContext");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database connection failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, exception: ex);
        }
        return new HealthCheckResult(HealthStatus.Healthy);
    }
}
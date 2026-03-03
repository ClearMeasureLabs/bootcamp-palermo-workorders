using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(DbContext dbContext, ILogger<CanConnectToDatabaseHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext hcContext,
        CancellationToken cancellationToken = new())
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                logger.LogDebug("Health check success via DbContext");
                return new HealthCheckResult(HealthStatus.Healthy);
            }
            else
            {
                logger.LogWarning("Health check failed: Cannot connect to database");
                return new HealthCheckResult(HealthStatus.Unhealthy, description: "Cannot connect to database");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database connection failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, exception: ex);
        }
    }
}
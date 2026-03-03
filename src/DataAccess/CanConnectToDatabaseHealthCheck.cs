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
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            
            if (canConnect)
            {
                logger.LogDebug("Database connection health check passed");
                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            
            logger.LogWarning("Database connection health check failed: unable to connect");
            return HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection health check encountered an exception");
            return HealthCheckResult.Unhealthy("Database connection failed with exception", ex);
        }
    }
}
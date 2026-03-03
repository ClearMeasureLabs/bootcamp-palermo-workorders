using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(
    DbContext dbContext,
    ILogger<CanConnectToDatabaseHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                const string message = "Database is unreachable";
                logger.LogWarning(message);
                return HealthCheckResult.Unhealthy(message);
            }

            logger.LogDebug("Health check success");
            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
            var message = $"Database connectivity check failed: {ex.Message}";
            logger.LogWarning(ex, message);
            return HealthCheckResult.Unhealthy(message, ex);
        }
    }
}

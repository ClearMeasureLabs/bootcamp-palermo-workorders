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
            var providerName = dbContext.Database.ProviderName ?? "Unknown";
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                logger.LogDebug("Health check success via DbContext");
                return new HealthCheckResult(HealthStatus.Healthy,
                    description: $"Database connection successful (Provider: {providerName})");
            }

            var description = $"Cannot connect to database (Provider: {providerName})";
            logger.LogWarning(description);
            return new HealthCheckResult(HealthStatus.Unhealthy, description: description);
        }
        catch (Exception ex)
        {
            var providerName = dbContext.Database.ProviderName ?? "Unknown";
            var description = $"Database connection failed (Provider: {providerName}): {ex.Message}";
            logger.LogWarning(ex, "Database connection failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, description: description, exception: ex);
        }
    }
}
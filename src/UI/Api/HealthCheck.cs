using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Api;

public class HealthCheck(ILogger<HealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        logger.LogDebug("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
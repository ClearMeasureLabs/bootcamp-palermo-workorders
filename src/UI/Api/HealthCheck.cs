using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// API-layer liveness probe registered under the <c>/api/health</c> endpoint.
/// </summary>
public class HealthCheck(ILogger<HealthCheck> logger) : IHealthCheck
{
    /// <summary>
    /// Returns a healthy result immediately; used as the liveness/readiness check for the container orchestrator.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        logger.LogDebug("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy("API layer is healthy"));
    }
}
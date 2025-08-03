using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Client.HealthChecks;

public class HealthCheckTracer(ILogger<HealthCheckTracer> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        logger.LogInformation("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy("UI.Client is healthy"));
    }
}
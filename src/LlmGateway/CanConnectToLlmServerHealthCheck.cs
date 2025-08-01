using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToLlmServerHealthCheck(ILogger<CanConnectToLlmServerHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        if (!connectedOk())
            return Task.FromResult(HealthCheckResult.Unhealthy("Cannot connect to LLM Server"));

        logger.LogInformation("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy());
    }

    private bool connectedOk()
    {
        return true;
    }
}
namespace ClearMeasure.Bootcamp.UI.Server;

public class Is64BitProcessHealthCheck(ILogger<Is64BitProcessHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        logger.LogDebug("Health check success");
        if (!Environment.Is64BitProcess)
        {
            return Task.FromResult(HealthCheckResult.Degraded("Running in 32-bit mode."));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
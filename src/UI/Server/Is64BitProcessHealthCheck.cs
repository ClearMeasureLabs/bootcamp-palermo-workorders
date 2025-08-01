namespace ClearMeasure.Bootcamp.UI.Server;

public class Is64BitProcessHealthCheck(ILogger<Is64BitProcessHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        logger.LogInformation($"Health check success");
        if (!Environment.Is64BitProcess)
        {
            return Task.FromResult(HealthCheckResult.Degraded());
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
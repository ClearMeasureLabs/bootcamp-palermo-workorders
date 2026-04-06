namespace ClearMeasure.Bootcamp.UI.Server;

public class Is64BitProcessHealthCheck(ILogger<Is64BitProcessHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        if (!Environment.Is64BitProcess)
        {
            var description = "Process is running as 32-bit. " +
                              "Expected 64-bit for optimal performance and memory utilization. " +
                              $"OS: {Environment.OSVersion}, ProcessorCount: {Environment.ProcessorCount}";
            logger.LogWarning(description);
            return Task.FromResult(HealthCheckResult.Degraded(description));
        }

        logger.LogDebug("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy("Process is running as 64-bit"));
    }
}
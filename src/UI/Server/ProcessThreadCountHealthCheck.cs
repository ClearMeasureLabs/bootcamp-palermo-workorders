using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Health check that reports the current process thread count as diagnostic data.
/// </summary>
public class ProcessThreadCountHealthCheck(ILogger<ProcessThreadCountHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var threadCount = Process.GetCurrentProcess().Threads.Count;
        logger.LogDebug("Process thread count: {ThreadCount}", threadCount);

        var data = new Dictionary<string, object> { ["threadCount"] = threadCount };
        return Task.FromResult(HealthCheckResult.Healthy(
            $"Process has {threadCount} threads",
            data: data));
    }
}

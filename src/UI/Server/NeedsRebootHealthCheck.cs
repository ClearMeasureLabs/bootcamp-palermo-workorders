using System.Threading;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Health check that reports <see cref="HealthStatus.Unhealthy"/> when <see cref="NeedsReboot"/>
/// is <c>true</c>, simulating memory corruption that requires a process restart.
/// Toggle via <c>/_demo/setneedsreboot/true|false</c>.
/// </summary>
public class NeedsRebootHealthCheck(ILogger<NeedsRebootHealthCheck> logger) : IHealthCheck
{
    private static int _needsReboot;

    /// <summary>
    /// Static flag toggled at runtime to simulate a corrupted process that needs a restart.
    /// Uses <see cref="Volatile"/> so HTTP handlers and health-check worker threads observe updates immediately.
    /// </summary>
    public static bool NeedsReboot
    {
        get => Volatile.Read(ref _needsReboot) != 0;
        set => Volatile.Write(ref _needsReboot, value ? 1 : 0);
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        if (NeedsReboot)
        {
            const string description = "memory is corrupted. Restart process";
            logger.LogWarning(description);
            return Task.FromResult(HealthCheckResult.Unhealthy(description));
        }

        logger.LogDebug("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy("Process memory is healthy"));
    }
}

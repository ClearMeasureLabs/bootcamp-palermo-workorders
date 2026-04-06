namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Health check that reports <see cref="HealthStatus.Unhealthy"/> when <see cref="NeedsReboot"/>
/// is <c>true</c>, simulating memory corruption that requires a process restart.
/// Toggle via <c>/_demo/setneedsreboot/true|false</c>.
/// </summary>
public class NeedsRebootHealthCheck(ILogger<NeedsRebootHealthCheck> logger) : IHealthCheck
{
    /// <summary>
    /// Static flag toggled at runtime to simulate a corrupted process that needs a restart.
    /// </summary>
    public static bool NeedsReboot { get; set; }

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

using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Client.HealthChecks;

public class ServerHealthCheck(IBus bus, ILogger<ServerHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        try
        {
            var status = await bus.Send(new ServerHealthCheckQuery());
            logger.LogInformation(status.ToString());
            return status == HealthStatus.Healthy
                ? new HealthCheckResult(status, "Server health checks passed")
                : new HealthCheckResult(status, $"Server health checks returned {status}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Server health check failed");
            return HealthCheckResult.Unhealthy(
                $"Server health check failed: {ex.Message}", ex);
        }
    }
}
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Client.HealthChecks;

public class ServerHealthCheck(IBus bus, ILogger<ServerHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        HealthStatus status = await bus.Send(new ServerHealthCheckQuery());
        logger.LogInformation(status.ToString());
        return new HealthCheckResult(status);
    }
}
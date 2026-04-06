using ClearMeasure.Bootcamp.Core;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Client.HealthChecks;

public class RemotableBusHealthCheck(IBus bus, ILogger<RemotableBusHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        try
        {
            IRequest<HealthStatus> remotableRequest = new HealthCheckRemotableRequest();
            var result = await bus.Send(remotableRequest);
            logger.LogInformation(result.ToString());
            return result == HealthStatus.Healthy
                ? new HealthCheckResult(result, "RemotableBus is healthy")
                : new HealthCheckResult(result, $"RemotableBus returned {result}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RemotableBus health check failed");
            return HealthCheckResult.Unhealthy(
                $"RemotableBus health check failed: {ex.Message}", ex);
        }
    }
}
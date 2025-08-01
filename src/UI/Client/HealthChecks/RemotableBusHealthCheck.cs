using ClearMeasure.Bootcamp.Core;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Client.HealthChecks;

public class RemotableBusHealthCheck(IBus bus, ILogger<RemotableBusHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        IRequest<HealthStatus> remotableRequest = new HealthCheckRemotableRequest(HealthStatus.Healthy);
        var result = await bus.Send(remotableRequest);
        logger.LogInformation(result.ToString());
        return new HealthCheckResult(result);
    }
}
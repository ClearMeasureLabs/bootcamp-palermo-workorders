using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Shared;

public class FunJeffreyCustomEventHealthCheck(
    TimeProvider time,
    ILogger<FunJeffreyCustomEventHealthCheck> logger) : IHealthCheck
{
    private static readonly ActivitySource HealthCheckActivitySource = new(TelemetryConstants.ApplicationSourceName);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        using var activity = HealthCheckActivitySource.StartActivity("JeffreyHealthCheckEvent");
        activity?.SetTag("time.minute_of_day", time.GetLocalNow().Minute);
        activity?.SetTag("time", time.GetLocalNow().ToString());

        logger.LogInformation("Health check success");
        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy));
    }
}
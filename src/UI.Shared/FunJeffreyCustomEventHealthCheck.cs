using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Shared;

public class FunJeffreyCustomEventHealthCheck(
    TelemetryClient telemetry,
    TimeProvider time,
    ILogger<FunJeffreyCustomEventHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var customEvent = new EventTelemetry("JeffreyHealthCheckEvent");
        customEvent.Metrics.Add("time minute of day", time.GetLocalNow().Minute);
        customEvent.Properties.Add("time", time.GetLocalNow().ToString());
        telemetry.TrackEvent(customEvent);

        logger.LogDebug("Health check success");
        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy));
    }
}
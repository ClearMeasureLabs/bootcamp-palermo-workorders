using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Shared;

public class FunJeffreyCustomEventHealthCheck(
    TimeProvider time,
    ILogger<FunJeffreyCustomEventHealthCheck> logger) : IHealthCheck
{
    public const string EventName = "JeffreyHealthCheckEvent";

    private static readonly ActivitySource ActivitySource = new("ChurchBulletin.Application", "1.0.0");

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var now = time.GetLocalNow();
        using var activity = ActivitySource.CreateActivity("JeffreyHealthCheck", ActivityKind.Internal)?.Start();
        activity?.AddEvent(new ActivityEvent(EventName, tags: new ActivityTagsCollection
        {
            ["time minute of day"] = now.Minute,
            ["time"] = now.ToString()
        }));

        logger.LogDebug("Health check success");
        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy));
    }
}

using System.Diagnostics;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class FunJeffreyCustomEventHealthCheckTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    [Test]
    public async Task CheckHealthAsync_WhenTraced_ShouldRecordJeffreyEventOnApplicationSource()
    {
        var now = new DateTimeOffset(2026, 10, 4, 9, 37, 0, TimeSpan.Zero);
        var stopped = new List<Activity>();
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == "ChurchBulletin.Application";
        listener.Sample = SampleAllData;
        listener.ActivityStopped = stopped.Add;
        ActivitySource.AddActivityListener(listener);
        var healthCheck = new FunJeffreyCustomEventHealthCheck(new FixedTimeProvider(now),
            NullLogger<FunJeffreyCustomEventHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        var healthEvent = stopped.SelectMany(a => a.Events)
            .Single(e => e.Name == FunJeffreyCustomEventHealthCheck.EventName);
        var tags = healthEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["time minute of day"].ShouldBe(37);
        tags["time"].ShouldBe(now.ToString());
    }

    [Test]
    public async Task CheckHealthAsync_WhenNotTraced_ShouldStillBeHealthy()
    {
        var healthCheck = new FunJeffreyCustomEventHealthCheck(TimeProvider.System,
            NullLogger<FunJeffreyCustomEventHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    private static ActivitySamplingResult SampleAllData(ref ActivityCreationOptions<ActivityContext> _) =>
        ActivitySamplingResult.AllData;
}

using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class DetailedHealthReportProviderTests
{
    private sealed class FixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    [Test]
    public void FromComponentStatuses_Should_MapStatusesAndOverall()
    {
        var fixedTime = new DateTime(2026, 3, 30, 10, 0, 0, DateTimeKind.Utc);
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy,
            ["DataAccess"] = HealthStatus.Unhealthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Unhealthy,
            new FixedUtcTimeProvider(fixedTime));

        detailed.CheckedAtUtc.ShouldBe(fixedTime);
        detailed.OverallStatus.ShouldBe(ComponentHealthStatus.Unhealthy);
        detailed.Components.Count.ShouldBe(2);
        detailed.Components.ShouldContain(c => c.Name == "API" && c.Status == ComponentHealthStatus.Healthy);
        detailed.Components.ShouldContain(c => c.Name == "DataAccess" && c.Status == ComponentHealthStatus.Unhealthy);
    }

    [Test]
    public void FromComponentStatuses_Should_OrderComponentsByName()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["Zed"] = HealthStatus.Healthy,
            ["Alpha"] = HealthStatus.Degraded
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Degraded,
            TimeProvider.System);

        detailed.Components[0].Name.ShouldBe("Alpha");
        detailed.Components[1].Name.ShouldBe("Zed");
    }
}

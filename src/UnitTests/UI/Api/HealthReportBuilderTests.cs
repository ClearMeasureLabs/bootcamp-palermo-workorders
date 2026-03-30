using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HealthReportBuilderTests
{
    private sealed class FixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    [Test]
    public void HealthReportBuilder_FromHealthReport_Should_MapEntriesAndOverall()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["LlmGateway"] = new(HealthStatus.Healthy, "ok", TimeSpan.FromMilliseconds(10), null, null),
            ["DataAccess"] = new(HealthStatus.Degraded, "slow", TimeSpan.FromMilliseconds(20), null, null),
            ["Server"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
            ["Jeffrey"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null)
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(30));

        var built = HealthReportBuilder.FromHealthReport(report, new FixedUtcTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        built.OverallStatus.ShouldBe(ComponentHealthStatus.Degraded);
        built.Components.Count.ShouldBe(5);
        var llm = built.Components.Single(c => c.Name == "LlmGateway");
        llm.Status.ShouldBe(ComponentHealthStatus.Healthy);
        llm.Description.ShouldBe("ok");
        llm.Duration.ShouldBe(TimeSpan.FromMilliseconds(10));
        var data = built.Components.Single(c => c.Name == "DataAccess");
        data.Status.ShouldBe(ComponentHealthStatus.Degraded);
        data.Description.ShouldBe("slow");
    }

    [Test]
    public void HealthReportBuilder_FromHealthReport_Should_MarkMissingAsUnhealthy()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["LlmGateway"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
                ["DataAccess"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
                ["Server"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
                ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null)
            },
            TimeSpan.Zero);

        var built = HealthReportBuilder.FromHealthReport(report);

        built.Components.Single(c => c.Name == "Jeffrey").Status.ShouldBe(ComponentHealthStatus.Unhealthy);
        built.OverallStatus.ShouldBe(ComponentHealthStatus.Unhealthy);
    }

    [Test]
    public void HealthReportBuilder_MapStatus_Should_MapEnumStrings()
    {
        HealthReportBuilder.MapStatus(HealthStatus.Healthy).ShouldBe(ComponentHealthStatus.Healthy);
        HealthReportBuilder.MapStatus(HealthStatus.Degraded).ShouldBe(ComponentHealthStatus.Degraded);
        HealthReportBuilder.MapStatus(HealthStatus.Unhealthy).ShouldBe(ComponentHealthStatus.Unhealthy);
    }

    [Test]
    public void HealthReportBuilder_Build_Should_SetOverallStatus_FromWorstComponent()
    {
        var healthyOnly = new[]
        {
            new ComponentHealthEntry { Name = "A", Status = ComponentHealthStatus.Healthy },
            new ComponentHealthEntry { Name = "B", Status = ComponentHealthStatus.Healthy }
        };
        HealthReportBuilder.AggregateWorst(healthyOnly).ShouldBe(ComponentHealthStatus.Healthy);

        var mixed = new[]
        {
            new ComponentHealthEntry { Name = "A", Status = ComponentHealthStatus.Healthy },
            new ComponentHealthEntry { Name = "B", Status = ComponentHealthStatus.Degraded }
        };
        HealthReportBuilder.AggregateWorst(mixed).ShouldBe(ComponentHealthStatus.Degraded);

        var withUnhealthy = new[]
        {
            new ComponentHealthEntry { Name = "A", Status = ComponentHealthStatus.Healthy },
            new ComponentHealthEntry { Name = "B", Status = ComponentHealthStatus.Unhealthy },
            new ComponentHealthEntry { Name = "C", Status = ComponentHealthStatus.Degraded }
        };
        HealthReportBuilder.AggregateWorst(withUnhealthy).ShouldBe(ComponentHealthStatus.Unhealthy);
    }

    [Test]
    public void HealthReportBuilder_FromHealthReport_Should_SetCheckedAtUtc()
    {
        var fixedTime = new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc);
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["LlmGateway"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
                ["DataAccess"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
                ["Server"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
                ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
                ["Jeffrey"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null)
            },
            TimeSpan.Zero);

        var built = HealthReportBuilder.FromHealthReport(report, new FixedUtcTimeProvider(fixedTime));

        built.CheckedAtUtc.ShouldBe(fixedTime);
        built.CheckedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }
}

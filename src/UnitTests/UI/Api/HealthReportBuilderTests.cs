using ClearMeasure.Bootcamp.UI.Api;
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
    public void HealthReportBuilder_FromEntries_Should_ReturnNonNullPayload()
    {
        var components = new[]
        {
            new ComponentHealthEntry { Name = "A", Status = ComponentHealthStatus.Healthy }
        };
        var report = HealthReportBuilder.FromEntries(TimeProvider.System, components);

        report.ShouldNotBeNull();
        report.Components.ShouldNotBeNull();
        report.Components.Count.ShouldBe(1);
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
    public void HealthReportBuilder_FromEntries_Should_SetCheckedAtUtc()
    {
        var fixedTime = new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc);
        var report = HealthReportBuilder.FromEntries(
            new FixedUtcTimeProvider(fixedTime),
            [new ComponentHealthEntry { Name = "X", Status = ComponentHealthStatus.Healthy }]);

        report.CheckedAtUtc.ShouldBe(fixedTime);
        report.CheckedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public void HealthReportBuilder_FromEntries_Should_AggregateWorstAcrossComponents()
    {
        var components = new[]
        {
            new ComponentHealthEntry { Name = "LlmGateway", Status = ComponentHealthStatus.Healthy },
            new ComponentHealthEntry { Name = "Jeffrey", Status = ComponentHealthStatus.Degraded }
        };
        var report = HealthReportBuilder.FromEntries(TimeProvider.System, components);

        report.OverallStatus.ShouldBe(ComponentHealthStatus.Degraded);
    }
}

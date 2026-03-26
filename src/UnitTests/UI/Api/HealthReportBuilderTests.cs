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
    public void HealthReportBuilder_Build_Should_ReturnNonNullPayload()
    {
        var report = HealthReportBuilder.Build();

        report.ShouldNotBeNull();
        report.Components.ShouldNotBeNull();
        report.Components.Count.ShouldBeGreaterThan(0);
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
    public void HealthReportBuilder_Build_Should_SetCheckedAtUtc()
    {
        var fixedTime = new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc);
        var report = HealthReportBuilder.Build(new FixedUtcTimeProvider(fixedTime));

        report.CheckedAtUtc.ShouldBe(fixedTime);
        report.CheckedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public void Should_Build_When_DefaultMockData_ExpectDegradedOverallBecauseJeffrey()
    {
        var report = HealthReportBuilder.Build();

        report.OverallStatus.ShouldBe(ComponentHealthStatus.Degraded);
        var names = report.Components.Select(c => c.Name).ToHashSet();
        names.ShouldContain("LlmGateway");
        names.ShouldContain("DataAccess");
        names.ShouldContain("Server");
        names.ShouldContain("API");
        names.ShouldContain("Jeffrey");
        foreach (var c in report.Components)
        {
            (c.Status == ComponentHealthStatus.Healthy
                || c.Status == ComponentHealthStatus.Degraded
                || c.Status == ComponentHealthStatus.Unhealthy).ShouldBeTrue();
        }
    }
}

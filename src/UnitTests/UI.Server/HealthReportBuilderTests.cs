using ClearMeasure.Bootcamp.UI.Server.Health;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HealthReportBuilderTests
{
    [Test]
    public void Should_Build_SetOverallToDegraded_WhenMockIncludesDegradedAndNoUnhealthy()
    {
        var report = HealthReportBuilder.Build();

        report.OverallStatus.ShouldBe("Degraded");
        report.Components.ShouldContain(c => c.Status == "Degraded");
        report.Components.ShouldNotContain(c => c.Status == "Unhealthy");
    }

    [Test]
    public void Should_Build_IncludeCheckedAtUtc_InUtc()
    {
        var before = DateTime.UtcNow;
        var report = HealthReportBuilder.Build();
        var after = DateTime.UtcNow;

        report.CheckedAtUtc.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        report.CheckedAtUtc.ShouldBeLessThanOrEqualTo(after.AddSeconds(1));
        report.CheckedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public void Should_Build_ListNamedComponents()
    {
        var report = HealthReportBuilder.Build();

        var names = report.Components.Select(c => c.Name).ToHashSet();
        names.ShouldContain("ApiLayer");
        names.ShouldContain("CacheConnectivity");
        names.ShouldContain("ExternalServices");
    }

    [Test]
    public void Should_WorstOverall_ReturnUnhealthy_WhenAnyUnhealthy()
    {
        var components = new ComponentHealthEntry[]
        {
            new("A", "Healthy"),
            new("B", "Degraded"),
            new("C", "Unhealthy"),
        };

        HealthReportBuilder.WorstOverall(components).ShouldBe("Unhealthy");
    }

    [Test]
    public void Should_WorstOverall_ReturnDegraded_WhenDegradedButNoUnhealthy()
    {
        var components = new ComponentHealthEntry[]
        {
            new("A", "Healthy"),
            new("B", "Degraded"),
        };

        HealthReportBuilder.WorstOverall(components).ShouldBe("Degraded");
    }

    [Test]
    public void Should_WorstOverall_ReturnHealthy_WhenAllHealthy()
    {
        var components = new ComponentHealthEntry[]
        {
            new("A", "Healthy"),
            new("B", "Healthy"),
        };

        HealthReportBuilder.WorstOverall(components).ShouldBe("Healthy");
    }

    [Test]
    public void Should_WorstOverall_TreatUnknownStatusAsUnhealthy()
    {
        var components = new ComponentHealthEntry[] { new("A", "Unknown") };

        HealthReportBuilder.WorstOverall(components).ShouldBe("Unhealthy");
    }
}

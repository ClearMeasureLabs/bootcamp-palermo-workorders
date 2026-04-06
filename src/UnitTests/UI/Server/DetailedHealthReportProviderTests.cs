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

    [Test]
    public void FromHealthReport_Should_IncludeDescriptionAndDuration()
    {
        var fixedTime = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["DataAccess"] = new(
                HealthStatus.Healthy,
                "Database connection successful",
                TimeSpan.FromMilliseconds(42),
                null,
                new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(42));

        var detailed = DetailedHealthReportProvider.FromHealthReport(
            report, new FixedUtcTimeProvider(fixedTime));

        detailed.Components.Count.ShouldBe(1);
        var component = detailed.Components[0];
        component.Name.ShouldBe("DataAccess");
        component.Status.ShouldBe(ComponentHealthStatus.Healthy);
        component.Description.ShouldBe("Database connection successful");
        component.DurationMs.ShouldBe(42);
        component.ExceptionMessage.ShouldBeNull();
        component.ExceptionDetail.ShouldBeNull();
        component.Data.ShouldBeNull();
    }

    [Test]
    public void FromHealthReport_Should_IncludeExceptionDetailsWhenUnhealthy()
    {
        var fixedTime = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var exception = new InvalidOperationException("Connection refused");
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["DataAccess"] = new(
                HealthStatus.Unhealthy,
                "Database connection failed",
                TimeSpan.FromMilliseconds(150),
                exception,
                new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(150));

        var detailed = DetailedHealthReportProvider.FromHealthReport(
            report, new FixedUtcTimeProvider(fixedTime));

        var component = detailed.Components[0];
        component.Status.ShouldBe(ComponentHealthStatus.Unhealthy);
        component.Description.ShouldBe("Database connection failed");
        component.ExceptionMessage.ShouldBe("Connection refused");
        component.ExceptionDetail.ShouldNotBeNull();
        component.ExceptionDetail.ShouldContain("InvalidOperationException");
        component.ExceptionDetail.ShouldContain("Connection refused");
    }

    [Test]
    public void FromHealthReport_Should_IncludeDataDictionaryWhenPresent()
    {
        var fixedTime = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var data = new Dictionary<string, object>
        {
            ["Provider"] = "SqlServer",
            ["RetryCount"] = 3
        };
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["DataAccess"] = new(
                HealthStatus.Degraded,
                "Slow response",
                TimeSpan.FromMilliseconds(5000),
                null,
                data)
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(5000));

        var detailed = DetailedHealthReportProvider.FromHealthReport(
            report, new FixedUtcTimeProvider(fixedTime));

        var component = detailed.Components[0];
        component.Status.ShouldBe(ComponentHealthStatus.Degraded);
        component.Data.ShouldNotBeNull();
        component.Data!.Count.ShouldBe(2);
        component.Data["Provider"].ShouldBe("SqlServer");
        component.Data["RetryCount"].ShouldBe(3);
    }

    [Test]
    public void BuildComponentEntry_Should_PopulateAllFieldsFromEntry()
    {
        var exception = new TimeoutException("Timed out after 30s");
        var data = new Dictionary<string, object> { ["endpoint"] = "https://api.example.com" };
        var entry = new HealthReportEntry(
            HealthStatus.Unhealthy,
            "Service unavailable",
            TimeSpan.FromMilliseconds(30000),
            exception,
            data);

        var component = DetailedHealthReportProvider.BuildComponentEntry("ExternalService", entry);

        component.Name.ShouldBe("ExternalService");
        component.Status.ShouldBe(ComponentHealthStatus.Unhealthy);
        component.Description.ShouldBe("Service unavailable");
        component.DurationMs.ShouldBe(30000);
        component.ExceptionMessage.ShouldBe("Timed out after 30s");
        component.ExceptionDetail.ShouldNotBeNull();
        component.ExceptionDetail.ShouldContain("TimeoutException");
        component.Data.ShouldNotBeNull();
        component.Data!["endpoint"].ShouldBe("https://api.example.com");
    }
}

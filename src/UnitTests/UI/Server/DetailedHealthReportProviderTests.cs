using System.Diagnostics;
using System.Runtime.InteropServices;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// Live process memory is sampled once inside the provider and again by the test
    /// microseconds later. Any allocation — or a background GC — between the two samples
    /// shifts the value, and because both are rounded to whole megabytes, crossing a single
    /// 1 MiB boundary is enough to break exact equality (observed in CI: "should be 148 but
    /// was 147"). These assertions verify the property is wired to the corresponding reader,
    /// so compare against a fresh sample within a tolerance rather than demanding equality.
    /// </summary>
    private const int MemorySampleToleranceMb = 64;

    private static void ShouldTrackLiveSample(int reported, int freshSample) =>
        reported.ShouldBeInRange(
            freshSample - MemorySampleToleranceMb,
            freshSample + MemorySampleToleranceMb);

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
    public void FromComponentStatuses_Should_SetProcessId()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.ProcessId.ShouldBe(Environment.ProcessId);
    }

    [Test]
    public void FromComponentStatuses_Should_SetProcessorCount()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.ProcessorCount.ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public void FromComponentStatuses_Should_SetIs64BitProcess()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.Is64BitProcess.ShouldBe(Environment.Is64BitProcess);
    }

    [Test]
    public void TimeZoneIdSetFromComponentStatuses()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.TimeZoneId.ShouldBe(TimeZoneInfo.Local.Id);
    }

    [Test]
    public void FromComponentStatuses_Should_SetProcessPriority()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.ProcessPriority.ShouldBe(DetailedHealthReportProvider.GetProcessPriority());
        detailed.ProcessPriority.ShouldBe(Process.GetCurrentProcess().PriorityClass.ToString());
    }

    [Test]
    public void FromHealthReport_Should_SetProcessId()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.ProcessId.ShouldBe(Environment.ProcessId);
    }

    [Test]
    public void FromHealthReport_Should_SetProcessorCount()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.ProcessorCount.ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public void FromHealthReport_Should_SetIs64BitProcess()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.Is64BitProcess.ShouldBe(Environment.Is64BitProcess);
    }

    [Test]
    public void TimeZoneIdSetFromHealthReport()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.TimeZoneId.ShouldBe(TimeZoneInfo.Local.Id);
    }

    [Test]
    public void FromHealthReport_Should_SetProcessPriority()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.ProcessPriority.ShouldBe(DetailedHealthReportProvider.GetProcessPriority());
        detailed.ProcessPriority.ShouldBe(Process.GetCurrentProcess().PriorityClass.ToString());
    }

    [Test]
    public void FromComponentStatuses_Should_SetOsDescription()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.OsDescription.ShouldNotBeNull();
        detailed.OsDescription.ShouldNotBeEmpty();
        detailed.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
    }

    [Test]
    public void FromComponentStatuses_Should_SetFrameworkDescription()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.FrameworkDescription.ShouldNotBeNull();
        detailed.FrameworkDescription.ShouldNotBeEmpty();
        detailed.FrameworkDescription.ShouldBe(RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void FromComponentStatuses_Should_SetGcMemoryMb()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        ShouldTrackLiveSample(detailed.GcMemoryMb, DetailedHealthReportProvider.GetGcMemoryMb());
    }

    [Test]
    public void FromComponentStatuses_Should_SetWorkingSetMb()
    {
        var entries = new Dictionary<string, HealthStatus>(StringComparer.Ordinal)
        {
            ["API"] = HealthStatus.Healthy
        };

        var detailed = DetailedHealthReportProvider.FromComponentStatuses(
            entries,
            HealthStatus.Healthy,
            TimeProvider.System);

        detailed.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
        ShouldTrackLiveSample(detailed.WorkingSetMb, DetailedHealthReportProvider.GetWorkingSetMb());
    }

    [Test]
    public void FromHealthReport_Should_SetOsDescription()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.OsDescription.ShouldNotBeNull();
        detailed.OsDescription.ShouldNotBeEmpty();
        detailed.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
    }

    [Test]
    public void FromHealthReport_Should_SetFrameworkDescription()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.FrameworkDescription.ShouldNotBeNull();
        detailed.FrameworkDescription.ShouldNotBeEmpty();
        detailed.FrameworkDescription.ShouldBe(RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void FromHealthReport_Should_SetGcMemoryMb()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        ShouldTrackLiveSample(detailed.GcMemoryMb, DetailedHealthReportProvider.GetGcMemoryMb());
    }

    [Test]
    public void FromHealthReport_Should_SetWorkingSetMb()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var detailed = DetailedHealthReportProvider.FromHealthReport(report, TimeProvider.System);

        detailed.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
        ShouldTrackLiveSample(detailed.WorkingSetMb, DetailedHealthReportProvider.GetWorkingSetMb());
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
    public async Task DetailedHealthReportProvider_GetAsync_Should_AggregateAllNonLiveChecks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("LiveProbe", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck("API", () => HealthCheckResult.Healthy())
            .AddCheck("DataAccess", () => HealthCheckResult.Degraded("Slow"));
        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var sut = new DetailedHealthReportProvider(healthCheckService, TimeProvider.System);

        var report = await sut.GetReportAsync();

        var names = report.Components.Select(c => c.Name).ToHashSet();
        names.ShouldContain("API");
        names.ShouldContain("DataAccess");
        names.ShouldNotContain("LiveProbe");
        report.OverallStatus.ShouldBe(ComponentHealthStatus.Degraded);
    }

    [Test]
    public async Task DetailedHealthReportProvider_GetAsync_Should_MapComponentEntries_WithNameStatusDescriptionDurationMs()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("DataAccess", () => HealthCheckResult.Healthy("Database connection successful"));
        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var sut = new DetailedHealthReportProvider(healthCheckService, TimeProvider.System);

        var report = await sut.GetReportAsync();

        var component = report.Components.Single(c => c.Name == "DataAccess");
        component.Status.ShouldBe(ComponentHealthStatus.Healthy);
        component.Description.ShouldBe("Database connection successful");
        component.DurationMs.ShouldNotBeNull();
        component.DurationMs!.Value.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task DetailedHealthReportProvider_GetAsync_Should_CaptureExceptionFields_When_CheckFails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("DataAccess", () => HealthCheckResult.Unhealthy(
                "Database connection failed",
                new InvalidOperationException("Connection refused")));
        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var sut = new DetailedHealthReportProvider(healthCheckService, TimeProvider.System);

        var report = await sut.GetReportAsync();

        var component = report.Components.Single(c => c.Name == "DataAccess");
        component.Status.ShouldBe(ComponentHealthStatus.Unhealthy);
        component.ExceptionMessage.ShouldBe("Connection refused");
        component.ExceptionDetail.ShouldNotBeNull();
        component.ExceptionDetail.ShouldContain("InvalidOperationException");
    }

    [Test]
    public async Task DetailedHealthReportProvider_GetAsync_Should_SortComponentsByName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("Zed", () => HealthCheckResult.Healthy())
            .AddCheck("Alpha", () => HealthCheckResult.Degraded());
        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var sut = new DetailedHealthReportProvider(healthCheckService, TimeProvider.System);

        var report = await sut.GetReportAsync();

        report.Components[0].Name.ShouldBe("Alpha");
        report.Components[1].Name.ShouldBe("Zed");
    }

    [Test]
    public async Task DetailedHealthReportProvider_GetAsync_Should_IncludeDataField_When_CheckReturnsMetadata()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("DataAccess", () => HealthCheckResult.Degraded(
                "Slow response",
                data: new Dictionary<string, object> { ["Provider"] = "SqlServer", ["RetryCount"] = 3 }));
        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var sut = new DetailedHealthReportProvider(healthCheckService, TimeProvider.System);

        var report = await sut.GetReportAsync();

        var component = report.Components.Single(c => c.Name == "DataAccess");
        component.Data.ShouldNotBeNull();
        component.Data!["Provider"].ShouldBe("SqlServer");
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

using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DetailedHealthEtagFingerprintTests
{
    [Test]
    public void FromReport_Should_OmitVolatileFields_When_BuildingFingerprint()
    {
        var report = new DetailedHealthReport
        {
            OverallStatus = ComponentHealthStatus.Degraded,
            CheckedAtUtc = DateTime.UtcNow,
            ProcessId = 1,
            OsDescription = "os",
            FrameworkDescription = "fw",
            GcMemoryMb = 10,
            WorkingSetMb = 20,
            ProcessorCount = 2,
            Is64BitProcess = true,
            TimeZoneId = "UTC",
            ProcessPriority = "Normal",
            Components =
            [
                new ComponentHealthEntry
                {
                    Name = "API",
                    Status = ComponentHealthStatus.Healthy,
                    Description = "ok",
                    DurationMs = 12.5,
                    ExceptionDetail = "should-not-fingerprint"
                },
                new ComponentHealthEntry
                {
                    Name = "DataAccess",
                    Status = ComponentHealthStatus.Degraded,
                    ExceptionMessage = "slow",
                    Data = new Dictionary<string, object> { ["Provider"] = "SqlServer" }
                }
            ]
        };

        var fingerprint = DetailedHealthEtagFingerprint.FromReport(report);

        fingerprint.OverallStatus.ShouldBe(ComponentHealthStatus.Degraded);
        fingerprint.Components.Count.ShouldBe(2);
        fingerprint.Components[0].Name.ShouldBe("API");
        fingerprint.Components[0].Description.ShouldBe("ok");
        fingerprint.Components[1].Name.ShouldBe("DataAccess");
        fingerprint.Components[1].ExceptionMessage.ShouldBe("slow");
        fingerprint.Components[1].Data.ShouldNotBeNull();
        var data = fingerprint.Components[1].Data!;
        data[0].Key.ShouldBe("Provider");
        data[0].Value.ShouldBe("SqlServer");
    }

    [Test]
    public void FromReport_Should_ProduceSameEtag_When_OnlyVolatileFieldsDiffer()
    {
        var baseComponents = new[]
        {
            new ComponentHealthEntry { Name = "API", Status = ComponentHealthStatus.Healthy, DurationMs = 1 }
        };
        var a = new DetailedHealthReport
        {
            OverallStatus = ComponentHealthStatus.Healthy,
            CheckedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ProcessId = 1,
            OsDescription = "a",
            FrameworkDescription = "a",
            GcMemoryMb = 1,
            WorkingSetMb = 1,
            ProcessorCount = 1,
            Is64BitProcess = true,
            TimeZoneId = "UTC",
            ProcessPriority = "Normal",
            Components = baseComponents
        };
        var b = new DetailedHealthReport
        {
            OverallStatus = ComponentHealthStatus.Healthy,
            CheckedAtUtc = new DateTime(2026, 8, 26, 15, 0, 0, DateTimeKind.Utc),
            ProcessId = 99,
            OsDescription = "b",
            FrameworkDescription = "b",
            GcMemoryMb = 999,
            WorkingSetMb = 999,
            ProcessorCount = 8,
            Is64BitProcess = false,
            TimeZoneId = "America/Chicago",
            ProcessPriority = "High",
            Components =
            [
                new ComponentHealthEntry { Name = "API", Status = ComponentHealthStatus.Healthy, DurationMs = 999 }
            ]
        };

        var etagA = ConditionalGetEtag.CreateWeakEtagForJson(DetailedHealthEtagFingerprint.FromReport(a));
        var etagB = ConditionalGetEtag.CreateWeakEtagForJson(DetailedHealthEtagFingerprint.FromReport(b));

        etagA.ShouldBe(etagB);
    }
}

using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DetailedHealthEtagFingerprintTests
{
    private static DetailedHealthReport BuildReport(DateTime checkedAtUtc, double durationA, double durationB)
    {
        return new DetailedHealthReport
        {
            OverallStatus = ComponentHealthStatus.Degraded,
            CheckedAtUtc = checkedAtUtc,
            Components =
            [
                new ComponentHealthEntry
                {
                    Name = "Alpha",
                    Status = ComponentHealthStatus.Healthy,
                    DurationMs = durationA,
                    ExceptionDetail = "stacktrace-alpha"
                },
                new ComponentHealthEntry
                {
                    Name = "Beta",
                    Status = ComponentHealthStatus.Degraded,
                    DurationMs = durationB,
                    ExceptionDetail = "stacktrace-beta"
                }
            ]
        };
    }

    [Test]
    public void FromReport_Should_ProduceEquivalentJson_When_OnlyVolatileFieldsDiffer()
    {
        var a = DetailedHealthEtagFingerprint.FromReport(
            BuildReport(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1.5, 2.0));
        var b = DetailedHealthEtagFingerprint.FromReport(
            BuildReport(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc), 99.0, 0.1));

        JsonSerializer.Serialize(a, ConditionalGetEtag.JsonSerializerOptions)
            .ShouldBe(JsonSerializer.Serialize(b, ConditionalGetEtag.JsonSerializerOptions));
    }

    [Test]
    public void FromReport_Should_ProduceDifferentJson_When_ComponentStatusChanges()
    {
        static DetailedHealthReport oneStatus(string componentStatus)
        {
            return new DetailedHealthReport
            {
                OverallStatus = componentStatus,
                CheckedAtUtc = DateTime.UtcNow,
                Components =
                [
                    new ComponentHealthEntry { Name = "X", Status = componentStatus }
                ]
            };
        }

        var degraded = DetailedHealthEtagFingerprint.FromReport(oneStatus(ComponentHealthStatus.Degraded));
        var healthy = DetailedHealthEtagFingerprint.FromReport(oneStatus(ComponentHealthStatus.Healthy));

        JsonSerializer.Serialize(degraded, ConditionalGetEtag.JsonSerializerOptions)
            .ShouldNotBe(JsonSerializer.Serialize(healthy, ConditionalGetEtag.JsonSerializerOptions));
    }
}

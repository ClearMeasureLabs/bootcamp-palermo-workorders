using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MetricsControllerTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRuntimeMetricsCollector(
        long totalRequests,
        MetricsSummaryResponse? summaryOverride = null) : IRuntimeMetricsCollector
    {
        private long _totalRequests = totalRequests;

        public void RecordRequest() => Interlocked.Increment(ref _totalRequests);

        public MetricsSummaryResponse BuildSummary(TimeProvider timeProvider) =>
            summaryOverride ?? new MetricsSummaryResponse(
                SimpleHealthResponseBuilder.Build(timeProvider).Uptime,
                _totalRequests,
                42,
                128,
                new GcCollectionCounts(10, 3, 1));
    }

    [Test]
    public void GetSummary_Should_ReturnJson_WithUptimeAndTotalRequests_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var collector = new StubRuntimeMetricsCollector(1234);
        var controller = new MetricsController(clock, collector)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.GetSummary();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
        payload.TotalRequests.ShouldBe(1234);
        payload.GcMemoryMb.ShouldBe(42);
        payload.WorkingSetMb.ShouldBe(128);
        payload.GcCollectionCounts.Gen0.ShouldBe(10);
        payload.GcCollectionCounts.Gen1.ShouldBe(3);
        payload.GcCollectionCounts.Gen2.ShouldBe(1);
    }

    [Test]
    public void GetSummary_Should_SerializeWithCamelCasePropertyNames_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var fixedSummary = new MetricsSummaryResponse(
            TimeSpan.FromHours(1),
            99,
            10,
            20,
            new GcCollectionCounts(1, 2, 3));
        var collector = new StubRuntimeMetricsCollector(0, fixedSummary);
        var controller = new MetricsController(clock, collector)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.GetSummary();
        var content = result.ShouldBeOfType<ContentResult>();

        using var doc = JsonDocument.Parse(content.Content!);
        var root = doc.RootElement;
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequests", out _).ShouldBeTrue();
        root.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        root.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        root.TryGetProperty("gcCollectionCounts", out var gcCounts).ShouldBeTrue();
        gcCounts.TryGetProperty("gen0", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen1", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen2", out _).ShouldBeTrue();
        root.TryGetProperty("environment", out _).ShouldBeFalse();
        root.TryGetProperty("featureFlags", out _).ShouldBeFalse();
    }
}

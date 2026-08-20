using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MetricsSummaryControllerTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRequestMetrics(long total) : IRequestMetrics
    {
        public long TotalRequestsServed => total;

        public void Increment()
        {
        }
    }

    [Test]
    public void GetSummary_Should_ReturnOk_WithExpectedJsonShape()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var controller = new MetricsSummaryController(clock, new StubRequestMetrics(0))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.GetSummary();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollectionCounts", out var gcCounts).ShouldBeTrue();
        gcCounts.TryGetProperty("gen0", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen1", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen2", out _).ShouldBeTrue();
    }

    [Test]
    public void GetSummary_Should_IncludeRequestCount_FromIRequestMetrics()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        const long expectedTotal = 12345;
        var controller = new MetricsSummaryController(clock, new StubRequestMetrics(expectedTotal))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.GetSummary();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.TotalRequestsServed.ShouldBe(expectedTotal);
    }
}

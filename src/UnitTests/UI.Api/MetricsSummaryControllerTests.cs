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

    private sealed class StubRequestMetricsSnapshot(long totalRequestsServed) : IRequestMetricsSnapshot
    {
        public long TotalRequestsServed { get; } = totalRequestsServed;
    }

    [Test]
    public void Should_return_200_with_metrics_json()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var snapshot = new StubRequestMetricsSnapshot(100);
        var controller = new MetricsSummaryController(clock, snapshot)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcHeapMemoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcGen0Collections", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcGen1Collections", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcGen2Collections", out _).ShouldBeTrue();
    }

    [Test]
    public void Should_return_positive_request_count()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        const long expectedCount = 1234;
        var snapshot = new StubRequestMetricsSnapshot(expectedCount);
        var controller = new MetricsSummaryController(clock, snapshot)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.TotalRequestsServed.ShouldBe(expectedCount);
        payload.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(0);
    }
}

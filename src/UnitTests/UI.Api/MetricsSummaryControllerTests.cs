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

    private sealed class StubRequestMetricsStore(long totalRequests) : IRequestMetricsStore
    {
        public long TotalRequests { get; } = totalRequests;

        public void Increment()
        {
        }
    }

    [Test]
    public void Should_ReturnJsonContent_With_MetricsShape_When_Get()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var store = new StubRequestMetricsStore(7);
        var controller = new MetricsSummaryController(clock, store)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.TotalRequests.ShouldBe(7);
        payload.Uptime.ShouldBe(MetricsSummaryBuilder.Build(clock, store).Uptime);
        payload.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        payload.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Should_ExposeContentType_ApplicationJson_When_Get()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var store = new StubRequestMetricsStore(0);
        var controller = new MetricsSummaryController(clock, store)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
    }
}

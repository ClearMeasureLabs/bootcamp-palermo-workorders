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

    private sealed class StubMetricsSnapshot(MetricsSummaryResponse response) : IApplicationRuntimeMetricsSnapshot
    {
        public void RecordRequest()
        {
        }

        public MetricsSummaryResponse Build(TimeProvider timeProvider) => response;
    }

    [Test]
    public void GetSummary_Should_ReturnJson_WithExpectedFields_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var stubPayload = new MetricsSummaryResponse(
            TimeSpan.FromHours(2),
            42,
            9_000_000,
            8_000_000,
            new GcCollectionCounts(10, 3, 1));
        var controller = new MetricsSummaryController(clock, new StubMetricsSnapshot(stubPayload))
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
        payload!.Uptime.ShouldBe(TimeSpan.FromHours(2));
        payload.TotalRequestsServed.ShouldBe(42);
        payload.WorkingSetBytes.ShouldBe(9_000_000);
        payload.GcTotalMemoryBytes.ShouldBe(8_000_000);
        payload.GcCollections.Gen0.ShouldBe(10);
        payload.GcCollections.Gen1.ShouldBe(3);
        payload.GcCollections.Gen2.ShouldBe(1);
    }

    [Test]
    public void GetSummary_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var stubPayload = new MetricsSummaryResponse(
            TimeSpan.FromMinutes(5),
            1,
            100,
            200,
            new GcCollectionCounts(1, 0, 0));
        var httpContext = new DefaultHttpContext();
        var controller = new MetricsSummaryController(clock, new StubMetricsSnapshot(stubPayload))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var first = controller.GetSummary();
        var etag = httpContext.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrWhiteSpace();

        httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etag;
        controller = new MetricsSummaryController(clock, new StubMetricsSnapshot(stubPayload))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var second = controller.GetSummary();
        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }
}

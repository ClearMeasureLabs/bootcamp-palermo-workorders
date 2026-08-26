using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

/// <summary>
/// Unit coverage for <see cref="MetricsController"/> (issue #9158 metrics summary contract).
/// </summary>
[TestFixture]
public class MetricsControllerTests
{
    private sealed class StubRequestCounter(long total) : IHttpRequestMetricsCounter
    {
        public long Total => total;
        public void Increment() { }
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void GetSummary_Should_ReturnJson_WithRequiredFields_When_Called()
    {
        var controller = CreateController(totalRequests: 7);

        var result = controller.GetSummary();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload.TotalRequestsServed.ShouldBe(7);
        payload.WorkingSetBytes.ShouldBeGreaterThan(0);
        payload.ManagedMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen0Collections.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen1Collections.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen2Collections.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void GetSummary_Should_SetWeakEtag_When_Called()
    {
        var controller = CreateController(totalRequests: 3);

        controller.GetSummary();

        var etag = controller.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();
        etag.ShouldStartWith("W/");
    }

    [Test]
    public void GetSummary_Should_Return304_When_IfNoneMatchIsAny()
    {
        var controller = CreateController(totalRequests: 3);
        controller.Request.Headers.IfNoneMatch = "*";

        var result = controller.GetSummary();

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
        controller.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
    }

    private static MetricsController CreateController(long totalRequests)
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
        return new MetricsController(clock, new StubRequestCounter(totalRequests))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}

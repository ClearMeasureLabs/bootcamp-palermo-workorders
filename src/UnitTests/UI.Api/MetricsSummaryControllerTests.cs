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

    private sealed class StubApplicationRequestMetrics(long totalRequests) : IApplicationRequestMetrics
    {
        public long TotalRequests { get; } = totalRequests;

        public void Increment() => throw new NotSupportedException();
    }

    private sealed class StubProcessRuntimeMetrics(long memoryUsageBytes, GcCollectionCounts gcCollections)
        : IProcessRuntimeMetrics
    {
        public long WorkingSetBytes { get; } = memoryUsageBytes;

        public GcCollectionCounts GcCollections { get; } = gcCollections;
    }

    [Test]
    public void Get_Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var requestMetrics = new StubApplicationRequestMetrics(42);
        var runtimeMetrics = new StubProcessRuntimeMetrics(52428800, new GcCollectionCounts(10, 5, 2));
        var httpContext = new DefaultHttpContext();
        var controller = new MetricsSummaryController(clock, requestMetrics, runtimeMetrics)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        _ = controller.Get();
        var etag = httpContext.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrWhiteSpace();

        var secondContext = new DefaultHttpContext();
        secondContext.Request.Headers.IfNoneMatch = etag;
        controller.ControllerContext = new ControllerContext { HttpContext = secondContext };

        var second = controller.Get();
        second.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    [Test]
    public void Get_Should_ReturnJson_WithUptimeRequestsMemoryAndGcCollections_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var requestMetrics = new StubApplicationRequestMetrics(42);
        var runtimeMetrics = new StubProcessRuntimeMetrics(52428800, new GcCollectionCounts(10, 5, 2));
        var controller = new MetricsSummaryController(clock, requestMetrics, runtimeMetrics)
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
        payload!.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
        payload.TotalRequests.ShouldBe(42);
        payload.MemoryUsageBytes.ShouldBeGreaterThan(0);
        payload.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
        controller.Response.Headers.ETag.ToString().ShouldNotBeNullOrWhiteSpace();
    }
}

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

    private sealed class StubHttpRequestMetrics(long total) : IHttpRequestMetrics
    {
        public long TotalRequestsServed => total;

        public void RecordRequest() => throw new InvalidOperationException("not used in unit test");
    }

    [Test]
    public void GetSummary_Should_ReturnJson_WithExpectedFields_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var metrics = new StubHttpRequestMetrics(42);
        var controller = new MetricsSummaryController(clock, metrics)
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
        payload.TotalRequestsServed.ShouldBe(42);
        payload.WorkingSetBytes.ShouldBeGreaterThan(0);
        payload.TotalAllocatedBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen0Collections.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen1Collections.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen2Collections.ShouldBeGreaterThanOrEqualTo(0);
    }
}

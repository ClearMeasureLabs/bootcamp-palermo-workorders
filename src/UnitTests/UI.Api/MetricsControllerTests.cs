using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

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
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
        var controller = new MetricsController(clock, new StubRequestCounter(7))
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
        payload.TotalRequestsServed.ShouldBe(7);
        payload.WorkingSetBytes.ShouldBeGreaterThan(0);
        payload.ManagedMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
    }
}

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

    private sealed class StubRequestMetricsCollector(long totalRequestsServed) : IRequestMetricsCollector
    {
        public void RecordRequest()
        {
        }

        public long TotalRequestsServed { get; } = totalRequestsServed;
    }

    [Test]
    public void Get_Should_ReturnJson_WithUptimeRequestsMemoryAndGc_When_Called()
    {
        const long totalRequests = 17;
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var collector = new StubRequestMetricsCollector(totalRequests);
        var controller = new MetricsSummaryController(clock, collector)
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
        payload.TotalRequestsServed.ShouldBe(totalRequests);
        payload.ManagedMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.WorkingSetBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }
}

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

    private sealed class StubRequestMetricsSnapshot(long totalRequests) : IRequestMetricsSnapshot
    {
        public long TotalRequestsServed => totalRequests;

        public void RecordRequest()
        {
        }

        public MetricsSummaryResponse BuildSummary(TimeProvider timeProvider) =>
            new RequestMetricsSnapshotProvider().BuildSummary(timeProvider) with
            {
                TotalRequestsServed = totalRequests
            };
    }

    [Test]
    public void Get_Should_ReturnJson_WithUptimeRequestsMemoryAndGcCounts_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var snapshot = new StubRequestMetricsSnapshot(42);
        var controller = new MetricsSummaryController(clock, snapshot)
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
        payload.TotalRequestsServed.ShouldBe(42);
        payload.WorkingSetMb.ShouldBeGreaterThan(0);
        payload.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
        payload.CapturedAtUtc.ShouldBe(clock.GetUtcNow().UtcDateTime);
    }
}

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

    private sealed class StubRequestCounters(long total) : IRequestCounters
    {
        public long TotalRequests { get; } = total;
        public void IncrementRequest() { }
    }

    private sealed class StubRuntimeMetrics : IProcessRuntimeMetrics
    {
        public long ManagedMemoryBytes { get; init; } = 42;
        public int GcGen0Collections { get; init; } = 1;
        public int GcGen1Collections { get; init; } = 2;
        public int GcGen2Collections { get; init; } = 3;
    }

    [Test]
    public void Get_Should_ReturnJson_WithUptimeTotalAndRuntimeFields_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var stubCounters = new StubRequestCounters(100);
        var stubRuntime = new StubRuntimeMetrics();
        var controller = new MetricsSummaryController(clock, stubCounters, stubRuntime)
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
        payload.TotalRequests.ShouldBe(100);
        payload.ManagedMemoryBytes.ShouldBe(42);
        payload.GcGen0Collections.ShouldBe(1);
        payload.GcGen1Collections.ShouldBe(2);
        payload.GcGen2Collections.ShouldBe(3);
    }
}

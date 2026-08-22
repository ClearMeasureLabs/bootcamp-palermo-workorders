using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MetricsSummaryBuilderTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRequestMetricsCounter(long total) : IRequestMetricsCounter
    {
        public long TotalRequestsServed { get; } = total;
        public void RecordRequest() { }
    }

    [Test]
    public void MetricsSummaryBuilder_Should_ReturnNonNegativeUptime_When_TimeProviderFixed()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var counter = new StubRequestMetricsCounter(0);

        var response = MetricsSummaryBuilder.Build(clock, start, counter);

        response.Uptime.ShouldBe(TimeSpan.FromHours(2));
        response.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Test]
    public void MetricsSummaryBuilder_Should_ExposeMemoryAndGcCollections_When_Built()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 8, 20, 11, 0, 0, TimeSpan.Zero);
        var counter = new StubRequestMetricsCounter(0);

        var response = MetricsSummaryBuilder.Build(clock, start, counter);

        response.Memory.GcMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        response.Memory.WorkingSetBytes.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void MetricsSummaryBuilder_Should_IncludeTotalRequests_FromCounter()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 8, 20, 11, 0, 0, TimeSpan.Zero);
        var counter = new StubRequestMetricsCounter(42);

        var response = MetricsSummaryBuilder.Build(clock, start, counter);

        response.TotalRequestsServed.ShouldBe(42);
    }
}

[TestFixture]
public class RequestMetricsCounterTests
{
    [Test]
    public void RequestMetricsCounter_Should_Increment_When_Recorded()
    {
        var counter = new RequestMetricsCounter();

        counter.RecordRequest();
        counter.RecordRequest();
        counter.RecordRequest();

        counter.TotalRequestsServed.ShouldBe(3);
    }
}

[TestFixture]
public class MetricsSummaryControllerTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRequestMetricsCounter(long total) : IRequestMetricsCounter
    {
        public long TotalRequestsServed { get; } = total;
        public void RecordRequest() { }
    }

    [Test]
    public void MetricsSummaryController_Get_Should_Return200Json_WithRequiredProperties()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero));
        var controller = new MetricsSummaryController(clock, new StubRequestMetricsCounter(7))
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
        doc.RootElement.TryGetProperty("memory", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollections", out _).ShouldBeTrue();
    }
}

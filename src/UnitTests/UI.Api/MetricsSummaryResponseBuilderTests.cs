using System.Diagnostics;
using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MetricsSummaryResponseBuilderTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRequestMetricsSnapshot(long totalRequestsServed) : IRequestMetricsSnapshot
    {
        public long TotalRequestsServed { get; } = totalRequestsServed;
    }

    [Test]
    public void Should_build_response_with_correct_uptime()
    {
        var processStartUtc = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime();
        var clock = new FixedUtcTimeProvider(processStartUtc.AddHours(1));
        var snapshot = new StubRequestMetricsSnapshot(42);

        var response = MetricsSummaryResponseBuilder.Build(clock, snapshot);

        response.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        response.Uptime.ShouldBe(TimeSpan.FromHours(1));
    }

    [Test]
    public void Should_include_gc_collection_counts()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var snapshot = new StubRequestMetricsSnapshot(0);

        var response = MetricsSummaryResponseBuilder.Build(clock, snapshot);

        response.GcGen0Collections.ShouldBeGreaterThanOrEqualTo(0);
        response.GcGen1Collections.ShouldBeGreaterThanOrEqualTo(0);
        response.GcGen2Collections.ShouldBeGreaterThanOrEqualTo(0);
        response.GcGen0Collections.ShouldBe(GC.CollectionCount(0));
        response.GcGen1Collections.ShouldBe(GC.CollectionCount(1));
        response.GcGen2Collections.ShouldBe(GC.CollectionCount(2));
    }

    [Test]
    public void Should_calculate_memory_in_megabytes()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var snapshot = new StubRequestMetricsSnapshot(0);

        var response = MetricsSummaryResponseBuilder.Build(clock, snapshot);

        response.GcHeapMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        response.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
        response.GcHeapMemoryMb.ShouldBe(MetricsSummaryResponseBuilder.GetGcMemoryMb());
        response.WorkingSetMb.ShouldBe(MetricsSummaryResponseBuilder.GetWorkingSetMb());
    }
}

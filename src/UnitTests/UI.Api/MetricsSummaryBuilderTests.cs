using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MetricsSummaryBuilderTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRequestMetrics(long totalRequestsServed) : IRequestMetrics
    {
        public long TotalRequestsServed { get; } = totalRequestsServed;

        public void IncrementTotalRequestsServed() =>
            throw new NotSupportedException();
    }

    [Test]
    public void Build_Returns_ValidMetricsSummaryResponse_When_Called()
    {
        var clock = new FixedUtcTimeProvider(DateTimeOffset.UtcNow.AddMinutes(5));
        var metrics = new StubRequestMetrics(100);

        var response = MetricsSummaryBuilder.Build("Testing", clock, metrics);

        response.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        response.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(0);
        response.MemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollectionCounts.Gen0Count.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollectionCounts.Gen1Count.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollectionCounts.Gen2Count.ShouldBeGreaterThanOrEqualTo(0);
        response.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
    }

    [Test]
    public void Build_IncludesGcCollectionCounts_When_Called()
    {
        var clock = new FixedUtcTimeProvider(DateTimeOffset.UtcNow.AddMinutes(5));
        var metrics = new StubRequestMetrics(0);

        var response = MetricsSummaryBuilder.Build("Testing", clock, metrics);

        response.GcCollectionCounts.Gen0Count.ShouldBe(GC.CollectionCount(0));
        response.GcCollectionCounts.Gen1Count.ShouldBe(GC.CollectionCount(1));
        response.GcCollectionCounts.Gen2Count.ShouldBe(GC.CollectionCount(2));
    }

    [Test]
    public void Build_HandlesDifferentTimeProviders_When_CalledWithFixedAndSystemTime()
    {
        var fixedClock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var fixedMetrics = new StubRequestMetrics(0);
        var fixedResponse = MetricsSummaryBuilder.Build("Testing", fixedClock, fixedMetrics);
        fixedResponse.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(fixedClock).Uptime);

        var systemMetrics = new StubRequestMetrics(0);
        var systemResponse = MetricsSummaryBuilder.Build("Testing", TimeProvider.System, systemMetrics);
        var expectedUptime = SimpleHealthResponseBuilder.Build(TimeProvider.System).Uptime;
        systemResponse.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        systemResponse.Uptime.ShouldBeInRange(
            expectedUptime - TimeSpan.FromSeconds(1),
            expectedUptime + TimeSpan.FromSeconds(1));
    }
}

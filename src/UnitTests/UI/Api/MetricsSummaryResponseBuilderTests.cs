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

    private sealed class StubApplicationRequestMetrics(long totalRequests) : IApplicationRequestMetrics
    {
        public long TotalRequests { get; } = totalRequests;

        public void Increment() => throw new NotSupportedException();
    }

    [Test]
    public void Build_Should_IncludeRequestCount_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 3, 30, 14, 0, 0, TimeSpan.Zero));
        var requestMetrics = new StubApplicationRequestMetrics(17);
        var runtimeMetrics = new ProcessRuntimeMetrics();

        var response = MetricsSummaryResponseBuilder.Build(clock, requestMetrics, runtimeMetrics);

        response.TotalRequests.ShouldBe(17);
    }

    [Test]
    public void Build_Should_IncludePositiveMemoryUsage_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 3, 30, 14, 0, 0, TimeSpan.Zero));
        var requestMetrics = new StubApplicationRequestMetrics(1);
        var runtimeMetrics = new ProcessRuntimeMetrics();

        var response = MetricsSummaryResponseBuilder.Build(clock, requestMetrics, runtimeMetrics);

        response.MemoryUsageBytes.ShouldBeGreaterThan(0);
    }

    [Test]
    public void Build_Should_IncludeGcCollectionCounts_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 3, 30, 14, 0, 0, TimeSpan.Zero));
        var requestMetrics = new StubApplicationRequestMetrics(1);
        var runtimeMetrics = new ProcessRuntimeMetrics();

        var response = MetricsSummaryResponseBuilder.Build(clock, requestMetrics, runtimeMetrics);

        response.GcCollections.Gen0.ShouldBe(GC.CollectionCount(0));
        response.GcCollections.Gen1.ShouldBe(GC.CollectionCount(1));
        response.GcCollections.Gen2.ShouldBe(GC.CollectionCount(2));
        response.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(response.GcCollections.Gen1);
        response.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(response.GcCollections.Gen2);
    }
}

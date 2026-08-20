using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ApplicationRuntimeMetricsBuilderTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRequestMetrics(long total) : IRequestMetrics
    {
        public long TotalRequestsServed => total;

        public void Increment()
        {
        }
    }

    [Test]
    public void Build_Should_ComputeUptime_FromTimeProviderAndProcessStart()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero);
        var metrics = new StubRequestMetrics(0);

        var response = ApplicationRuntimeMetricsBuilder.Build(clock, start, metrics);

        response.Uptime.ShouldBe(TimeSpan.FromHours(3));
    }

    [Test]
    public void Build_Should_SetNonNegativeMemoryMb_When_Called()
    {
        var metrics = new StubRequestMetrics(0);

        var response = ApplicationRuntimeMetricsBuilder.Build(TimeProvider.System, metrics);

        response.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        response.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Build_Should_SetGcCollectionCounts_FromRuntime_When_Called()
    {
        var metrics = new StubRequestMetrics(42);

        var response = ApplicationRuntimeMetricsBuilder.Build(TimeProvider.System, metrics);

        response.GcCollectionCounts.Gen0.ShouldBe(GC.CollectionCount(0));
        response.GcCollectionCounts.Gen1.ShouldBe(GC.CollectionCount(1));
        response.GcCollectionCounts.Gen2.ShouldBe(GC.CollectionCount(2));
        response.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
        response.TotalRequestsServed.ShouldBe(42);
    }
}

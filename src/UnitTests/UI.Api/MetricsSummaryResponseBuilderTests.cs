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

    [Test]
    public void Build_Should_ReuseUptimeSemantics_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));

        var response = MetricsSummaryResponseBuilder.Build(clock, 0);

        response.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
    }

    [Test]
    public void Build_Should_ExposeGcCollectionCounts_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));

        var response = MetricsSummaryResponseBuilder.Build(clock, 0);

        response.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Build_Should_ExposeMemoryValues_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));

        var response = MetricsSummaryResponseBuilder.Build(clock, 0);

        response.ManagedMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        response.WorkingSetBytes.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Build_Should_PassThroughTotalRequestsServed_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));

        var response = MetricsSummaryResponseBuilder.Build(clock, 42);

        response.TotalRequestsServed.ShouldBe(42);
    }
}

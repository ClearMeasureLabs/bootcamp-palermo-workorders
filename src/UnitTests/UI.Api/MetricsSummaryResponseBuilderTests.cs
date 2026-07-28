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
    public void Build_Should_ComputeUptime_FromFixedProcessStart_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 3, 30, 14, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 3, 30, 12, 30, 0, TimeSpan.Zero);

        var response = MetricsSummaryResponseBuilder.Build(clock, totalRequestsServed: 0, start);

        response.Uptime.ShouldBe(TimeSpan.FromHours(1.5));
        response.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock, start).Uptime);
        response.TotalRequestsServed.ShouldBe(0);
    }

    [Test]
    public void Build_Should_ExposeNonNegativeMemoryAndGcCounts_When_Called()
    {
        var response = MetricsSummaryResponseBuilder.Build(TimeProvider.System, totalRequestsServed: 7);

        response.CurrentMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }
}

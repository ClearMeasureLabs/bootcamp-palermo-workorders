using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MetricsSummaryBuilderTests
{
    private sealed class StubRequestCounter(long total) : IHttpRequestMetricsCounter
    {
        public long Total { get; private set; } = total;
        public void Increment() => Total++;
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void Build_Should_ReturnUptimeAndRequestCount_When_StartAndCounterKnown()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 8, 26, 11, 0, 0, TimeSpan.Zero);
        var counter = new StubRequestCounter(42);

        var summary = MetricsSummaryBuilder.Build(clock, counter, start);

        summary.Uptime.ShouldBe(TimeSpan.FromHours(1));
        summary.TotalRequestsServed.ShouldBe(42);
        summary.WorkingSetBytes.ShouldBeGreaterThan(0);
        summary.ManagedMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        summary.GcGen0Collections.ShouldBeGreaterThanOrEqualTo(0);
        summary.GcGen1Collections.ShouldBeGreaterThanOrEqualTo(0);
        summary.GcGen2Collections.ShouldBeGreaterThanOrEqualTo(0);
    }
}

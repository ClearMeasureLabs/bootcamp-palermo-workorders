using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RuntimeMetricsCollectorTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void BuildSummary_Should_IncludeUptimeMemoryAndGcCounts_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var collector = new RuntimeMetricsCollector();

        var summary = collector.BuildSummary(clock);

        summary.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
        summary.TotalRequests.ShouldBe(0);
        summary.GcMemoryMb.ShouldBe(DetailedHealthReportProvider.GetGcMemoryMb());
        summary.WorkingSetMb.ShouldBe(DetailedHealthReportProvider.GetWorkingSetMb());
        summary.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        summary.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        summary.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void RecordRequest_Should_IncrementTotalRequests_When_Called()
    {
        var collector = new RuntimeMetricsCollector();

        collector.RecordRequest();
        collector.RecordRequest();

        collector.BuildSummary(TimeProvider.System).TotalRequests.ShouldBe(2);
    }
}

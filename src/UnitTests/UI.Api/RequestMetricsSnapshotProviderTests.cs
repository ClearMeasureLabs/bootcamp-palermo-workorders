using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class RequestMetricsSnapshotProviderTests
{
    [Test]
    public void RecordRequest_Should_IncrementTotalRequestsServed_When_CalledMultipleTimes()
    {
        var provider = new RequestMetricsSnapshotProvider();

        provider.RecordRequest();
        provider.RecordRequest();
        provider.RecordRequest();

        provider.TotalRequestsServed.ShouldBe(3);
    }

    [Test]
    public void BuildSummary_Should_ReturnLatestTotal_When_RequestsRecorded()
    {
        var provider = new RequestMetricsSnapshotProvider();
        provider.RecordRequest();
        provider.RecordRequest();

        var summary = provider.BuildSummary(TimeProvider.System);

        summary.TotalRequestsServed.ShouldBe(2);
        summary.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        summary.WorkingSetMb.ShouldBeGreaterThan(0);
        summary.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        summary.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
    }
}

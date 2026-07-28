using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsCollectorTests
{
    [Test]
    public void Should_increment_total_requests_when_request_completes()
    {
        var collector = new RequestMetricsCollector();

        collector.TotalRequestsServed.ShouldBe(0);

        collector.RecordRequest();
        collector.RecordRequest();
        collector.RecordRequest();

        collector.TotalRequestsServed.ShouldBe(3);
    }
}

using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsCollectorTests
{
    [Test]
    public void RecordRequest_Should_IncrementTotalRequestsServed_When_Called()
    {
        var collector = new RequestMetricsCollector();

        collector.RecordRequest();
        collector.RecordRequest();

        collector.TotalRequestsServed.ShouldBe(2);
    }

    [Test]
    public void RecordRequest_Should_IncrementTotalRequestsServed_When_CalledConcurrently()
    {
        var collector = new RequestMetricsCollector();
        const int iterations = 1000;

        Parallel.For(0, iterations, _ => collector.RecordRequest());

        collector.TotalRequestsServed.ShouldBe(iterations);
    }
}

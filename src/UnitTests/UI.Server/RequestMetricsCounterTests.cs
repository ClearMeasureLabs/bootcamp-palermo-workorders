using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsCounterTests
{
    [Test]
    public void RecordRequestServed_Should_IncrementTotalRequestsServed_When_CalledSequentially()
    {
        var counter = new RequestMetricsCounter();

        counter.TotalRequestsServed.ShouldBe(0);
        counter.RecordRequestServed();
        counter.TotalRequestsServed.ShouldBe(1);
        counter.RecordRequestServed();
        counter.RecordRequestServed();
        counter.TotalRequestsServed.ShouldBe(3);
    }

    [Test]
    public void RecordRequestServed_Should_BeThreadSafe_When_ConcurrentIncrements()
    {
        var counter = new RequestMetricsCounter();
        const int threads = 100;
        const int callsPerThread = 10;

        Parallel.For(0, threads, _ =>
        {
            for (var i = 0; i < callsPerThread; i++)
                counter.RecordRequestServed();
        });

        counter.TotalRequestsServed.ShouldBe(threads * callsPerThread);
    }
}

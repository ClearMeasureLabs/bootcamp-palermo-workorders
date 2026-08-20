using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class RequestMetricsTests
{
    [Test]
    public void Increment_Should_IncreaseTotalRequestsServed_When_Called()
    {
        var metrics = new RequestMetrics();

        metrics.Increment();

        metrics.TotalRequestsServed.ShouldBe(1);
    }

    [Test]
    public void Increment_Should_BeThreadSafe_When_CalledConcurrently()
    {
        var metrics = new RequestMetrics();
        const int threadCount = 16;
        const int incrementsPerThread = 1000;

        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < incrementsPerThread; i++)
            {
                metrics.Increment();
            }
        });

        metrics.TotalRequestsServed.ShouldBe(threadCount * incrementsPerThread);
    }
}

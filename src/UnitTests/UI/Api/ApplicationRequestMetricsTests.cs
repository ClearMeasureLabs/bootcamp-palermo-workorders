using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ApplicationRequestMetricsTests
{
    [Test]
    public void Increment_Should_IncreaseTotalRequests_When_Called()
    {
        var metrics = new ApplicationRequestMetrics();

        metrics.TotalRequests.ShouldBe(0);
        metrics.Increment();
        metrics.Increment();
        metrics.TotalRequests.ShouldBe(2);
    }
}

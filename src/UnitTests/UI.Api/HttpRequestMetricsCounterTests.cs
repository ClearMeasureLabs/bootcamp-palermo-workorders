using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HttpRequestMetricsCounterTests
{
    [Test]
    public void Increment_Should_IncreaseTotal_When_Called()
    {
        var counter = new HttpRequestMetricsCounter();
        counter.Total.ShouldBe(0);

        counter.Increment();
        counter.Increment();

        counter.Total.ShouldBe(2);
    }
}

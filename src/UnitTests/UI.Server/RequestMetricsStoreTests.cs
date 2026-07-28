using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsStoreTests
{
    [Test]
    public void Should_StartAtZero_When_Created()
    {
        var store = new RequestMetricsStore();

        store.TotalRequests.ShouldBe(0);
    }

    [Test]
    public async Task Should_Increment_ThreadSafe_When_CallIncrement()
    {
        var store = new RequestMetricsStore();

        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(store.Increment)));

        store.TotalRequests.ShouldBe(100);
    }
}

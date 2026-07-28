using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsMiddlewareTests
{
    [Test]
    public async Task Should_Increment_StoreOncePerRequest_When_RequestCompletes()
    {
        var store = new RequestMetricsStore();
        var middleware = new RequestMetricsMiddleware(_ => Task.CompletedTask, store);
        var context = new DefaultHttpContext();

        store.TotalRequests.ShouldBe(0);
        await middleware.InvokeAsync(context);
        store.TotalRequests.ShouldBe(1);
        await middleware.InvokeAsync(context);
        store.TotalRequests.ShouldBe(2);
    }
}

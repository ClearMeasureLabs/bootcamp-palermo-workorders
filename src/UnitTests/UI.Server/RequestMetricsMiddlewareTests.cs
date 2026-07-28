using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsMiddlewareTests
{
    [Test]
    public async Task Should_call_collector_on_each_request()
    {
        var collector = new RequestMetricsCollector();
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };
        var middleware = new RequestMetricsMiddleware(next, collector);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        invoked.ShouldBeTrue();
        collector.TotalRequestsServed.ShouldBe(1);
    }
}

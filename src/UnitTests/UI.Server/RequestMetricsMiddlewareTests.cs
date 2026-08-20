using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsMiddlewareTests
{
    [Test]
    public async Task Invoke_Should_IncrementRequestMetrics_When_RequestPassesThrough()
    {
        var metrics = new RequestMetrics();
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestMetricsMiddleware(next, metrics);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        metrics.TotalRequestsServed.ShouldBe(1);
    }

    [Test]
    public async Task Invoke_Should_ContinuePipeline_When_MetricsRecorded()
    {
        var metrics = new RequestMetrics();
        var called = false;
        RequestDelegate next = _ =>
        {
            called = true;
            return Task.CompletedTask;
        };
        var middleware = new RequestMetricsMiddleware(next, metrics);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        called.ShouldBeTrue();
    }
}

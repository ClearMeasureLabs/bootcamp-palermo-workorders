using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware;

[TestFixture]
public class HttpRequestMetricsMiddlewareTests
{
    private sealed class StubHttpRequestMetricsCounter : IHttpRequestMetricsCounter
    {
        public int IncrementCalls { get; private set; }

        public long Total => IncrementCalls;

        public void Increment() => IncrementCalls++;
    }

    [Test]
    public async Task InvokeAsync_FrameworkPath_DoesNotIncrementCounter()
    {
        var counter = new StubHttpRequestMetricsCounter();
        var nextCalled = false;
        var middleware = new HttpRequestMetricsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, counter);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_framework/blazor.web.js";

        await middleware.InvokeAsync(context);

        counter.IncrementCalls.ShouldBe(0);
        nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task InvokeAsync_ApiPath_IncrementsCounter()
    {
        var counter = new StubHttpRequestMetricsCounter();
        var nextCalled = false;
        var middleware = new HttpRequestMetricsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, counter);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/version";

        await middleware.InvokeAsync(context);

        counter.IncrementCalls.ShouldBe(1);
        nextCalled.ShouldBeTrue();
    }
}

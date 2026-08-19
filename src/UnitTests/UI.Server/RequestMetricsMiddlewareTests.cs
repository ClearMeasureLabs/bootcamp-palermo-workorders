using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsMiddlewareTests
{
    private sealed class StubRequestMetrics : IRequestMetrics
    {
        public long TotalRequestsServed { get; private set; }

        public void IncrementTotalRequestsServed() => TotalRequestsServed++;
    }

    [Test]
    public async Task InvokeAsync_IncrementsCounter_When_RequestCompletes()
    {
        var metrics = new StubRequestMetrics();
        var middleware = new RequestMetricsMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, metrics);

        metrics.TotalRequestsServed.ShouldBe(1);
    }

    [Test]
    public async Task InvokeAsync_IsThreadSafe_When_MultipleRequestsConcurrently()
    {
        var metrics = new RequestMetrics();
        var middleware = new RequestMetricsMiddleware(_ => Task.CompletedTask);
        const int requestCount = 20;

        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => middleware.InvokeAsync(new DefaultHttpContext(), metrics))
            .ToArray();

        await Task.WhenAll(tasks);

        metrics.TotalRequestsServed.ShouldBe(requestCount);
    }
}

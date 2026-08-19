using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsMiddlewareTests
{
    private sealed class StubRequestMetricsCollector : IRequestMetricsCollector
    {
        public int RecordRequestCallCount { get; private set; }

        public long TotalRequestsServed => RecordRequestCallCount;

        public void RecordRequest() => RecordRequestCallCount++;
    }

    [Test]
    public async Task InvokeAsync_Should_CallRecordRequestOnce_When_RequestPassesThrough()
    {
        var collector = new StubRequestMetricsCollector();
        var middleware = new RequestMetricsMiddleware(_ => Task.CompletedTask, collector);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        collector.RecordRequestCallCount.ShouldBe(1);
    }
}

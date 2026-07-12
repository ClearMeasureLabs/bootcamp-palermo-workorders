using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestMetricsMiddlewareTests
{
    private sealed class StubRequestMetricsCounter : IRequestMetricsCounter
    {
        public long TotalRequestsServed { get; private set; }

        public int RecordCalls { get; private set; }

        public void RecordRequestServed()
        {
            RecordCalls++;
            TotalRequestsServed++;
        }
    }

    [Test]
    public async Task InvokeAsync_Should_IncrementCounterOnce_When_PipelineCompletes()
    {
        var counter = new StubRequestMetricsCounter();
        var middleware = new RequestMetricsMiddleware(_ => Task.CompletedTask, counter);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        counter.RecordCalls.ShouldBe(1);
        counter.TotalRequestsServed.ShouldBe(1);
    }

    [Test]
    public async Task InvokeAsync_Should_IncrementCounterOnce_When_PipelineThrows()
    {
        var counter = new StubRequestMetricsCounter();
        var middleware = new RequestMetricsMiddleware(
            _ => throw new InvalidOperationException("pipeline failure"),
            counter);
        var context = new DefaultHttpContext();

        await Should.ThrowAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        counter.RecordCalls.ShouldBe(1);
        counter.TotalRequestsServed.ShouldBe(1);
    }
}

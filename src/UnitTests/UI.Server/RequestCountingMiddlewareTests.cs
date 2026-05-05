using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestCountingMiddlewareTests
{
    private sealed class StubCounters : IRequestCounters
    {
        public int InvocationCount { get; private set; }
        public long TotalRequests => InvocationCount;
        public void IncrementRequest() => InvocationCount++;
    }

    [Test]
    public async Task InvokeAsync_Should_IncrementCountersOncePerRequest_When_RequestCompletesPipeline()
    {
        var stubs = new StubCounters();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestCounters>(stubs);
        var provider = services.BuildServiceProvider();
        RequestDelegate downstream = ctx => Task.CompletedTask;
        var sut = new RequestCountingMiddleware(downstream);
        var ctx = new DefaultHttpContext();
        ctx.RequestServices = provider;

        await sut.InvokeAsync(ctx);

        stubs.InvocationCount.ShouldBe(1);
    }
}

using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestCountingMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_Should_IncrementSnapshot_When_RequestProcessed()
    {
        var snapshot = new RequestMetricsSnapshotProvider();
        var middleware = new RequestCountingMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, snapshot);

        snapshot.TotalRequestsServed.ShouldBe(1);
    }
}

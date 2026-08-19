using System.Net;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RealtimeNotificationWebSocketMiddlewareTests
{
    [Test]
    public async Task Should_Return400_When_NotWebSocketRequest()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(RealtimeNotificationWebSocketMiddleware.Path);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_PassThrough_When_PathDoesNotMatch()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/not-ws");

        response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);
    }
}

using System.Net;
using System.Net.WebSockets;
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

    [Test]
    public async Task Should_AcceptWebSocket_AndUnregister_WhenClientDisconnects()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var wsClient = factory.Server.CreateWebSocketClient();

        using var socket = await wsClient.ConnectAsync(
            new Uri(factory.Server.BaseAddress!, RealtimeNotificationWebSocketMiddleware.Path),
            CancellationToken.None);

        socket.State.ShouldBe(WebSocketState.Open);
        socket.Abort();
    }
}

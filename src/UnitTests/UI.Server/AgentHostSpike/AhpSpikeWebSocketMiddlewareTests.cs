using System.Net;
using System.Net.WebSockets;
using ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.AgentHostSpike;

[TestFixture]
public class AhpSpikeWebSocketMiddlewareTests
{
    [Test]
    public async Task Should_Return400_When_NotWebSocketRequest()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(AhpSpikeWebSocketMiddleware.Path);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_AcceptWebSocket_WhenClientConnects()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var wsClient = factory.Server.CreateWebSocketClient();

        using var socket = await wsClient.ConnectAsync(
            new Uri(factory.Server.BaseAddress, $"{AhpSpikeWebSocketMiddleware.Path}?user=alice"),
            CancellationToken.None);

        socket.State.ShouldBe(WebSocketState.Open);
        socket.Abort();
    }
}

using System.Net.WebSockets;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RealtimeNotificationHubTests
{
    [Test]
    public async Task BroadcastRemotableEventAsync_SendsToOpenConnections()
    {
        var hub = new RealtimeNotificationHub();
        var socket = new RecordingWebSocket(WebSocketState.Open);
        hub.Register(socket);

        await hub.BroadcastRemotableEventAsync(new UserLoggedInEvent("user1"));

        socket.SentPayloads.Count.ShouldBe(1);
        System.Text.Encoding.UTF8.GetString(socket.SentPayloads[0]).ShouldContain("remotableEvent");
    }

    [Test]
    public async Task BroadcastRemotableEventAsync_RemovesClosedConnections()
    {
        var hub = new RealtimeNotificationHub();
        hub.Register(new RecordingWebSocket(WebSocketState.Closed));

        await hub.BroadcastRemotableEventAsync(new UserLoggedInEvent("user1"));

        hub.ConnectionCount.ShouldBe(0);
    }

    private sealed class RecordingWebSocket : WebSocket
    {
        private readonly WebSocketState _state;

        public RecordingWebSocket(WebSocketState state) => _state = state;

        public List<byte[]> SentPayloads { get; } = [];

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;
        public override void Abort()
        {
        }
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public override void Dispose()
        {
        }
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            SentPayloads.Add(buffer.ToArray());
            return Task.CompletedTask;
        }
    }
}

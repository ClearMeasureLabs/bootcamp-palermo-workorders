// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable NotAccessedPositionalProperty.Local
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;

namespace ClearMeasure.Bootcamp.UI.Server.Notifications;

/// <summary>
/// Broadcasts <see cref="IRemotableEvent"/> payloads to connected WebSocket clients.
/// </summary>
public interface IRealtimeNotificationHub
{
    int ConnectionCount { get; }

    Guid Register(WebSocket socket);

    void Remove(Guid id);

    Task BroadcastRemotableEventAsync(IRemotableEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tracks WebSocket connections and broadcasts JSON notifications to all subscribers.
/// </summary>
public sealed class RealtimeNotificationHub : IRealtimeNotificationHub
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _connections = new();

    public int ConnectionCount => _connections.Count;

    public Guid Register(WebSocket socket)
    {
        var id = Guid.NewGuid();
        _connections[id] = socket;
        return id;
    }

    public void Remove(Guid id) => _connections.TryRemove(id, out _);

    public async Task BroadcastRemotableEventAsync(IRemotableEvent @event, CancellationToken cancellationToken = default)
    {
        var messageJson = new WebServiceMessage(@event).GetJson();
        var envelope = new RealtimeNotificationEnvelope(
            RealtimeNotificationKinds.RemotableEvent,
            messageJson);
        var json = JsonSerializer.Serialize(envelope, RealtimeNotificationHubJson.Options);
        await BroadcastUtf8JsonAsync(json, cancellationToken).ConfigureAwait(false);
    }

    private async Task BroadcastUtf8JsonAsync(string json, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        foreach (var (id, socket) in _connections.ToArray())
        {
            if (!await RealtimeWebSocketSender.TrySendAsync(socket, bytes, cancellationToken).ConfigureAwait(false))
            {
                _connections.TryRemove(id, out _);
            }
        }
    }
}

internal static class RealtimeWebSocketSender
{
    internal static async Task<bool> TrySendAsync(WebSocket socket, byte[] bytes, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return false;
        }

        try
        {
            await socket
                .SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Discriminator for real-time payloads.
/// </summary>
public static class RealtimeNotificationKinds
{
    public const string RemotableEvent = "remotableEvent";
}

/// <summary>
/// Wire envelope: <c>kind</c> plus <c>messageJson</c> (serialized <see cref="WebServiceMessage"/>).
/// </summary>
public sealed record RealtimeNotificationEnvelope(string Kind, string MessageJson);

internal static class RealtimeNotificationHubJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

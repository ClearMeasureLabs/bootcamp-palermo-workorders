using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AgentHostProtocol;

namespace ClearMeasure.Bootcamp.UI.Client.AgentHostSpike;

/// <summary>
/// Browser-WebSocket wiring around the AHP .NET client: opens a
/// <see cref="ClientWebSocket"/>, hosts the <see cref="AhpClient"/> via
/// <see cref="WebSocketTransport.FromClientWebSocket"/>, and pumps subscription events into
/// the <see cref="AhpSpikeChatProjection"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Browser WebSocket wiring; exercised by the AhpSpike acceptance test in a real browser")]
public sealed class AhpSpikeConnection : IAsyncDisposable
{
    private readonly Uri _endpoint;
    private readonly AhpSpikeChatProjection _projection;
    private readonly int _subscriptionBufferCapacity;
    private readonly Action _onChanged;
    private readonly Action<string> _log;
    private ClientWebSocket? _socket;
    private AhpClient? _client;

    /// <summary>Stable client id used for initialize and reconnect.</summary>
    public string ClientId { get; } = "wasm-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>Username echoed back by the spike host (from initialize <c>_meta</c>).</summary>
    public string? ServerUser { get; private set; }

    /// <summary>Protocol version negotiated during initialize.</summary>
    public string? NegotiatedVersion { get; private set; }

    /// <summary>Whether the underlying AHP connection is currently connected.</summary>
    public bool IsConnected => _client is { ConnectionState: ConnectionState.Connected };

    /// <summary>Creates a connection. Nothing is opened until <see cref="ConnectAsync"/> or
    /// <see cref="ReconnectAsync"/> is called.</summary>
    /// <param name="endpoint">WebSocket endpoint of the AHP host.</param>
    /// <param name="projection">Projection that receives snapshots and actions.</param>
    /// <param name="subscriptionBufferCapacity">AHP client subscription buffer capacity.</param>
    /// <param name="onChanged">Callback fired whenever the projection changes.</param>
    /// <param name="log">Callback receiving log lines.</param>
    public AhpSpikeConnection(
        Uri endpoint,
        AhpSpikeChatProjection projection,
        int subscriptionBufferCapacity,
        Action onChanged,
        Action<string> log)
    {
        _endpoint = endpoint;
        _projection = projection;
        _subscriptionBufferCapacity = subscriptionBufferCapacity;
        _onChanged = onChanged;
        _log = log;
    }

    /// <summary>Opens the WebSocket, attaches the spike subscription, and completes
    /// initialize, seeding the projection from the returned snapshot.</summary>
    public async Task ConnectAsync()
    {
        var sub = await OpenAsync();
        var init = await _client!.InitializeAsync(ClientId, ProtocolVersion.Supported, [AhpSpikeChatProjection.ChatUri]);
        NegotiatedVersion = init.ProtocolVersion;
        ServerUser = ReadServerUser(init.Meta);
        var snapshot = init.Snapshots.FirstOrDefault(s => s.Resource == AhpSpikeChatProjection.ChatUri)
            ?? throw new InvalidOperationException("initialize result has no snapshot for the spike chat");
        _projection.Seed(snapshot);
        _log($"initialize: protocol {NegotiatedVersion}, snapshot fromSeq {snapshot.FromSeq}, server saw user '{ServerUser}'");
        _ = PumpAsync(sub);
        _onChanged();
    }

    /// <summary>Dispatches a user <see cref="ChatTurnStartedAction"/> to start the demo stream.</summary>
    public async Task StartTurnAsync()
    {
        var turnId = "turn-" + Guid.NewGuid().ToString("N")[..8];
        await _client!.DispatchAsync(AhpSpikeChatProjection.ChatUri, new StateAction(new ChatTurnStartedAction
        {
            TurnId = turnId,
            StartedAt = DateTimeOffset.UtcNow.ToString("O"),
            Message = new Message
            {
                Text = "Stream 1,000 deltas",
                Origin = new MessageOrigin { Kind = MessageKind.User }
            }
        }));
        _log($"startTurn: dispatched ChatTurnStartedAction ({turnId})");
    }

    /// <summary>Aborts the socket to simulate a network drop.</summary>
    public void SimulateNetworkDrop()
    {
        _socket?.Abort();
        _log($"network drop simulated at serverSeq {_projection.LastSeenServerSeq}");
        _onChanged();
    }

    /// <summary>Drops the client and reconnects, replaying missed actions or reseeding from a
    /// fresh snapshot.</summary>
    public async Task ReconnectAsync()
    {
        await DisposeClientAsync();
        var sub = await OpenAsync();
        var lastSeen = _projection.LastSeenServerSeq;
        var result = await _client!.ReconnectAsync(ClientId, lastSeen, [AhpSpikeChatProjection.ChatUri]);
        ApplyReconnectResult(result, lastSeen);
        _ = PumpAsync(sub);
        _onChanged();
    }

    /// <summary>Disposes the AHP client if one is open.</summary>
    public async ValueTask DisposeAsync() => await DisposeClientAsync();

    private async Task DisposeClientAsync()
    {
        if (_client is null)
        {
            return;
        }

        try
        {
            await _client.DisposeAsync();
        }
        catch (Exception ex)
        {
            _log($"dispose: {ex.GetType().Name}");
        }

        _client = null;
    }

    private async Task<Subscription> OpenAsync()
    {
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(_endpoint, CancellationToken.None);
        _client = AhpClient.Connect(
            WebSocketTransport.FromClientWebSocket(_socket),
            new ClientConfig { SubscriptionBufferCapacity = _subscriptionBufferCapacity });
        return _client.AttachSubscription(AhpSpikeChatProjection.ChatUri);
    }

    private void ApplyReconnectResult(ReconnectResult result, long lastSeen)
    {
        if (result.Value is ReconnectReplayResult replay)
        {
            foreach (var a in replay.Actions)
            {
                _projection.Apply(a, fromReplay: true);
            }

            _log($"reconnect: replayed {replay.Actions.Count} actions (serverSeq {lastSeen + 1}..{_projection.LastSeenServerSeq})");
            return;
        }

        if (result.Value is ReconnectSnapshotResult snap)
        {
            foreach (var s in snap.Snapshots.Where(s => s.Resource == AhpSpikeChatProjection.ChatUri))
            {
                _projection.Seed(s);
            }

            _log("reconnect: server sent a fresh snapshot");
        }
    }

    private async Task PumpAsync(Subscription sub)
    {
        try
        {
            await foreach (var ev in sub.Events.ReadAllAsync())
            {
                switch (ev)
                {
                    case SubscriptionEventAction a:
                        _projection.Apply(a.Envelope);
                        break;
                    case SubscriptionEventSnapshot s:
                        _projection.Seed(s.Snapshot);
                        break;
                }

                _onChanged();
            }
        }
        catch (Exception ex)
        {
            _log($"pump stopped: {ex.GetType().Name}");
        }

        _log("subscription closed");
        _onChanged();
    }

    private static string? ReadServerUser(Dictionary<string, JsonElement>? meta)
    {
        if (meta is not null && meta.TryGetValue("spike.user", out var element))
        {
            return element.GetString();
        }

        return null;
    }
}

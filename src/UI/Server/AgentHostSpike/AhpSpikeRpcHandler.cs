using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.AgentHostProtocol;

namespace ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;

/// <summary>
/// JSON-RPC 2.0 message handler for one AHP WebSocket connection.
/// </summary>
public sealed class AhpSpikeRpcHandler : IDisposable
{
    private readonly AhpSpikeJournal _journal;
    private readonly AhpSpikeTurnStreamer _streamer;
    private readonly Action<string> _send;
    private readonly Action<ActionEnvelope> _listener;
    private readonly string _user;
    private string? _clientId;

    /// <summary>
    /// Creates the handler for one connection.
    /// </summary>
    public AhpSpikeRpcHandler(
        AhpSpikeJournal journal,
        AhpSpikeTurnStreamer streamer,
        string user,
        Action<string> send)
    {
        _journal = journal;
        _streamer = streamer;
        _send = send;
        _user = user;
        _listener = OnAction;
    }

    /// <summary>
    /// Client id supplied by the most recent initialize or reconnect, if any.
    /// </summary>
    public string? ClientId => _clientId;

    /// <summary>
    /// Processes one inbound JSON-RPC frame; invalid JSON is ignored.
    /// </summary>
    public void Handle(string frame)
    {
        try
        {
            using var document = JsonDocument.Parse(frame);
            var root = document.RootElement;
            if (!root.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
            {
                return;
            }

            var method = methodElement.GetString()!;
            JsonElement? id = root.TryGetProperty("id", out var idElement) ? idElement.Clone() : null;
            JsonElement? parameters = root.TryGetProperty("params", out var parametersElement) ? parametersElement.Clone() : null;
            Dispatch(method, id, parameters);
        }
        catch (JsonException)
        {
        }
    }

    /// <summary>
    /// Detaches the listener from the journal.
    /// </summary>
    public void Dispose() => _journal.Detach(_listener);

    private void Dispatch(string method, JsonElement? id, JsonElement? parameters)
    {
        switch (method)
        {
            case "initialize":
                HandleInitialize(id, parameters);
                break;
            case "reconnect":
                HandleReconnect(id, parameters);
                break;
            case "subscribe":
                HandleSubscribe(id, parameters);
                break;
            case "ping":
                SendEmptyResult(id);
                break;
            case "unsubscribe":
                _journal.Detach(_listener);
                break;
            case "dispatchAction":
                HandleDispatchAction(parameters);
                break;
            default:
                if (id is not null)
                {
                    SendError(id, -32601, $"Method not found: {method}");
                }
                break;
        }
    }

    private void HandleInitialize(JsonElement? id, JsonElement? parameters)
    {
        if (parameters is null)
        {
            SendError(id, -32602, "Missing params");
            return;
        }

        var paramsValue = SystemTextJsonAhpSerializer.Default.Deserialize<InitializeParams>(parameters.Value);
        var version = ProtocolVersion.Negotiate(paramsValue.ProtocolVersions);
        if (version is null)
        {
            SendError(id, -32005, "Unsupported protocol version", BuildSupportedVersionsData());
            return;
        }

        _clientId = paramsValue.ClientId;
        if (paramsValue.InitialSubscriptions is { Count: > 0 } subscriptions
            && subscriptions.Contains(AhpSpikeJournal.ChatUri))
        {
            _journal.Attach(_listener, snapshot =>
                SendResult(id, BuildInitializeResult(version, snapshot.FromSeq, [snapshot])));
            return;
        }

        SendResult(id, BuildInitializeResult(version, _journal.ServerSeq, []));
    }

    private void HandleReconnect(JsonElement? id, JsonElement? parameters)
    {
        if (parameters is null)
        {
            SendError(id, -32602, "Missing params");
            return;
        }

        var paramsValue = SystemTextJsonAhpSerializer.Default.Deserialize<ReconnectParams>(parameters.Value);
        _clientId = paramsValue.ClientId;
        var missing = paramsValue.Subscriptions
            .Where(channel => channel != AhpSpikeJournal.ChatUri)
            .ToList();
        if (!paramsValue.Subscriptions.Contains(AhpSpikeJournal.ChatUri))
        {
            SendResult(id, new ReconnectReplayResult { Actions = [], Missing = missing });
            return;
        }

        _journal.Resume(
            paramsValue.LastSeenServerSeq,
            _listener,
            actions => SendResult(id, new ReconnectReplayResult { Actions = actions.ToList(), Missing = missing }),
            snapshot => SendResult(id, new ReconnectSnapshotResult { Snapshots = [snapshot] }));
    }

    private void HandleSubscribe(JsonElement? id, JsonElement? parameters)
    {
        if (parameters is null)
        {
            SendError(id, -32602, "Missing params");
            return;
        }

        var paramsValue = SystemTextJsonAhpSerializer.Default.Deserialize<SubscribeParams>(parameters.Value);
        if (paramsValue.Channel != AhpSpikeJournal.ChatUri)
        {
            SendError(id, -32602, $"Unknown channel: {paramsValue.Channel}");
            return;
        }

        _journal.Attach(_listener, snapshot => SendResult(id, new SubscribeResult { Snapshot = snapshot }));
    }

    private void HandleDispatchAction(JsonElement? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        var paramsValue = SystemTextJsonAhpSerializer.Default.Deserialize<DispatchActionParams>(parameters.Value);
        if (paramsValue.Action.Value is not ChatTurnStartedAction started)
        {
            return;
        }

        _streamer.TryStart(
            started,
            new ActionOrigin { ClientId = _clientId ?? "unknown", ClientSeq = paramsValue.ClientSeq });
    }

    private void OnAction(ActionEnvelope envelope) =>
        SendFrame(writer =>
        {
            writer.WriteString("method", "action");
            writer.WritePropertyName("params");
            SystemTextJsonAhpSerializer.Default.SerializeToElement(envelope).WriteTo(writer);
        });

    private void SendResult<T>(JsonElement? id, T result) =>
        SendFrame(writer =>
        {
            WriteId(writer, id);
            writer.WritePropertyName("result");
            SystemTextJsonAhpSerializer.Default.SerializeToElement(result).WriteTo(writer);
        });

    private void SendEmptyResult(JsonElement? id) =>
        SendFrame(writer =>
        {
            WriteId(writer, id);
            writer.WritePropertyName("result");
            writer.WriteStartObject();
            writer.WriteEndObject();
        });

    private void SendError(JsonElement? id, int code, string message, JsonElement? data = null) =>
        SendFrame(writer =>
        {
            WriteId(writer, id);
            writer.WritePropertyName("error");
            writer.WriteStartObject();
            writer.WriteNumber("code", code);
            writer.WriteString("message", message);
            if (data is { } dataValue)
            {
                writer.WritePropertyName("data");
                dataValue.WriteTo(writer);
            }

            writer.WriteEndObject();
        });

    private void SendFrame(Action<Utf8JsonWriter> writeBody)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("jsonrpc", "2.0");
            writeBody(writer);
            writer.WriteEndObject();
        }

        _send(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    private static void WriteId(Utf8JsonWriter writer, JsonElement? id)
    {
        if (id is { } value)
        {
            writer.WritePropertyName("id");
            value.WriteTo(writer);
        }
    }

    private InitializeResult BuildInitializeResult(string version, long serverSeq, List<Snapshot> snapshots) =>
        new()
        {
            ProtocolVersion = version,
            ServerSeq = serverSeq,
            ServerInfo = new Implementation { Name = "church-bulletin-ahp-spike-host", Version = "0.1.0" },
            Meta = new Dictionary<string, JsonElement> { ["spike.user"] = BuildUserElement() },
            Snapshots = snapshots
        };

    private JsonElement BuildUserElement() => JsonSerializer.SerializeToElement(_user);

    private static JsonElement BuildSupportedVersionsData()
    {
        var versions = string.Join(",", ProtocolVersion.Supported.Select(v => "\"" + v + "\""));
        var json = "{\"supportedVersions\":[" + versions + "]}";
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

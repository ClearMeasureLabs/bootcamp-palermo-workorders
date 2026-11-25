using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;

/// <summary>
/// Accepts AHP spike WebSocket upgrades at <see cref="Path"/> and pumps JSON-RPC frames between
/// the socket and an <see cref="AhpSpikeRpcHandler"/>.
/// </summary>
public sealed class AhpSpikeWebSocketMiddleware(
    RequestDelegate next,
    AhpSpikeJournal journal,
    AhpSpikeTurnStreamer streamer)
{
    /// <summary>
    /// WebSocket path served by this middleware.
    /// </summary>
    public const string Path = "/ws/agent-host-spike";

    /// <summary>
    /// Routes spike WebSocket requests to <see cref="RunConnectionAsync"/> and passes everything else on.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.Equals(Path, StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await RunConnectionAsync(context).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage(Justification = "Socket pump; covered by the AhpSpike acceptance test in a real browser")]
    private async Task RunConnectionAsync(HttpContext context)
    {
        var user = context.Request.Query["user"].ToString();
        if (user.Length == 0)
        {
            user = "anonymous";
        }

        var cancellationToken = context.RequestAborted;
        var outbound = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
        var handler = new AhpSpikeRpcHandler(journal, streamer, user, frame => outbound.Writer.TryWrite(frame));
        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var writerTask = WriteLoopAsync(socket, outbound.Reader, cancellationToken);
        try
        {
            await ReceiveLoopAsync(socket, handler, cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            handler.Dispose();
            outbound.Writer.TryComplete();
            try
            {
                await writerTask.ConfigureAwait(false);
            }
            catch (WebSocketException)
            {
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Socket pump; covered by the AhpSpike acceptance test in a real browser")]
    private static async Task WriteLoopAsync(
        WebSocket socket,
        ChannelReader<string> reader,
        CancellationToken cancellationToken)
    {
        await foreach (var frame in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await socket
                .SendAsync(Encoding.UTF8.GetBytes(frame), WebSocketMessageType.Text, endOfMessage: true, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Socket pump; covered by the AhpSpike acceptance test in a real browser")]
    private static async Task ReceiveLoopAsync(
        WebSocket socket,
        AhpSpikeRpcHandler handler,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            using (var message = new MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                handler.Handle(Encoding.UTF8.GetString(message.ToArray()));
            }
        }
    }
}

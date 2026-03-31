using System.Net.WebSockets;

namespace ClearMeasure.Bootcamp.UI.Server.Notifications;

/// <summary>
/// Accepts WebSocket upgrades at <see cref="Path"/> and keeps connections open until the client closes.
/// </summary>
public sealed class RealtimeNotificationWebSocketMiddleware(
    RequestDelegate next,
    IRealtimeNotificationHub hub,
    ILogger<RealtimeNotificationWebSocketMiddleware> logger)
{
    public const string Path = "/ws/notifications";

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

        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var id = hub.Register(socket);

        try
        {
            // Server-push: do not block in ReceiveAsync (TestHost deadlocks with client Receive + server Send).
            await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException ex)
        {
            logger.LogDebug(ex, "WebSocket connection ended.");
        }
        finally
        {
            hub.Remove(id);
        }
    }
}

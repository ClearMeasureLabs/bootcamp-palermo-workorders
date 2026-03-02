using System.Collections.Concurrent;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe signaling mechanism for the NServiceBus tracer bullet health check.
/// The health check registers a correlation ID and waits; the reply handler
/// completes the signal when the reply arrives from the Worker endpoint.
/// </summary>
public static class TracerBulletSignal
{
    private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> Signals = new();

    /// <summary>
    /// Registers a correlation ID and returns a task that completes when the reply arrives.
    /// </summary>
    public static async Task WaitForReply(Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Signals[correlationId] = tcs;

        try
        {
            await tcs.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            Signals.TryRemove(correlationId, out _);
        }
    }

    /// <summary>
    /// Called by <see cref="Handlers.TracerBulletReplyHandler"/> when a reply arrives.
    /// Completes the <see cref="TaskCompletionSource{TResult}"/> so the waiting health check unblocks.
    /// </summary>
    public static void Complete(Guid correlationId)
    {
        if (Signals.TryGetValue(correlationId, out var tcs))
        {
            tcs.TrySetResult(true);
        }
    }
}

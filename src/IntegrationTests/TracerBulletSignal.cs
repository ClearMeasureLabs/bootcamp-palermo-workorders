using System.Collections.Concurrent;

namespace ClearMeasure.Bootcamp.IntegrationTests;

/// <summary>
/// Thread-safe signaling mechanism for the NServiceBus tracer bullet test.
/// The acceptance test registers a correlation ID and waits; the reply handler
/// completes the signal when the reply arrives from the Worker endpoint.
/// </summary>
public static class TracerBulletSignal
{
    private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> Signals = new();

    /// <summary>
    /// Registers a correlation ID and waits for the corresponding reply to arrive.
    /// </summary>
    public static async Task WaitForReply(Guid correlationId, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Signals[correlationId] = tcs;

        try
        {
            await tcs.Task.WaitAsync(timeout);
        }
        finally
        {
            Signals.TryRemove(correlationId, out _);
        }
    }

    /// <summary>
    /// Called by <see cref="Handlers.TracerBulletReplyHandler"/> when a reply arrives.
    /// Completes the <see cref="TaskCompletionSource{TResult}"/> so the waiting test unblocks.
    /// </summary>
    public static void Complete(Guid correlationId)
    {
        if (Signals.TryGetValue(correlationId, out var tcs))
        {
            tcs.TrySetResult(true);
        }
    }
}

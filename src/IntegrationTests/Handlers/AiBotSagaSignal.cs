using System.Collections.Concurrent;

namespace ClearMeasure.Bootcamp.IntegrationTests.Handlers;

/// <summary>
/// Thread-safe signaling mechanism for the AI bot saga integration test.
/// The test registers a work order number and waits; the handler
/// completes the signal when <see cref="Worker.Sagas.AiBotWorkerOrder.Events.AiBotCompletedWorkOrderEvent"/> arrives.
/// </summary>
public static class AiBotSagaSignal
{
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> Signals = new();

    /// <summary>
    /// Registers a work order number and waits for the saga to complete.
    /// </summary>
    public static async Task WaitForCompletion(string workOrderNumber, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Signals[workOrderNumber] = tcs;

        try
        {
            await tcs.Task.WaitAsync(timeout);
        }
        finally
        {
            Signals.TryRemove(workOrderNumber, out _);
        }
    }

    /// <summary>
    /// Called by <see cref="AiBotCompletedTestHandler"/> when the saga completes.
    /// Completes the <see cref="TaskCompletionSource{TResult}"/> so the waiting test unblocks.
    /// </summary>
    public static void Complete(string workOrderNumber)
    {
        if (Signals.TryGetValue(workOrderNumber, out var tcs))
        {
            tcs.TrySetResult(true);
        }
    }
}

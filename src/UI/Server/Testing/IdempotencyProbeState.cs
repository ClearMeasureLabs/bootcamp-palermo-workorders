using System.Threading;

namespace ClearMeasure.Bootcamp.UI.Server.Testing;

/// <summary>
/// Mutable counter for <c>/api/_test/idempotency-probe</c> (Testing environment only).
/// </summary>
public sealed class IdempotencyProbeState
{
    private int _counter;

    /// <summary>
    /// Increments and returns the new value.
    /// </summary>
    public int Next() => Interlocked.Increment(ref _counter);
}

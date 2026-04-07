using System.Threading;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Default <see cref="IRequestMetrics"/> using <see cref="Interlocked"/> operations.
/// </summary>
public sealed class RequestMetrics : IRequestMetrics
{
    private long _count;

    /// <inheritdoc />
    public void Increment() => Interlocked.Increment(ref _count);

    /// <inheritdoc />
    public long TotalCount => Interlocked.Read(ref _count);
}

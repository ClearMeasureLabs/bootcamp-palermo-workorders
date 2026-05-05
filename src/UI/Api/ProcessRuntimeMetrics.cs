namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Reads live values from <see cref="GC"/> for <see cref="IProcessRuntimeMetrics"/>.
/// </summary>
public sealed class ProcessRuntimeMetrics : IProcessRuntimeMetrics
{
    /// <inheritdoc />
    public long ManagedMemoryBytes => GC.GetTotalMemory(false);

    /// <inheritdoc />
    public int GcGen0Collections => GC.CollectionCount(0);

    /// <inheritdoc />
    public int GcGen1Collections => GC.CollectionCount(1);

    /// <inheritdoc />
    public int GcGen2Collections => GC.CollectionCount(2);
}

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Reads live process memory and GC statistics.
/// </summary>
public sealed class ProcessRuntimeMetrics : IProcessRuntimeMetrics
{
    /// <inheritdoc />
    public long WorkingSetBytes => Environment.WorkingSet;

    /// <inheritdoc />
    public GcCollectionCounts GcCollections => new(
        Gen0: GC.CollectionCount(0),
        Gen1: GC.CollectionCount(1),
        Gen2: GC.CollectionCount(2));
}

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Snapshot of process memory and GC statistics for metrics reporting.
/// </summary>
public interface IProcessRuntimeMetrics
{
    /// <summary>
    /// Current process working set in bytes.
    /// </summary>
    long WorkingSetBytes { get; }

    /// <summary>
    /// GC collection counts by generation.
    /// </summary>
    GcCollectionCounts GcCollections { get; }
}

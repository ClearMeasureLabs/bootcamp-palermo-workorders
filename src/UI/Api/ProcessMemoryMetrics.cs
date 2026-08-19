namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Process memory metrics shared by health and metrics endpoints.
/// </summary>
public static class ProcessMemoryMetrics
{
    /// <summary>
    /// Returns the current GC heap size in megabytes (rounded).
    /// </summary>
    public static int GetGcMemoryMb() =>
        (int)Math.Round(GC.GetTotalMemory(false) / 1_048_576.0);

    /// <summary>
    /// Returns the current process working set in megabytes (rounded).
    /// </summary>
    public static int GetWorkingSetMb() =>
        (int)Math.Round(Environment.WorkingSet / 1_048_576.0);
}

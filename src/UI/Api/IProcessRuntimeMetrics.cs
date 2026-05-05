namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Point-in-time managed memory and GC collection counts from the current process BCL APIs.
/// </summary>
/// <remarks>
/// <para>
/// Implementations intentionally read GC state at invocation time (<see cref="GC.GetTotalMemory"/>
/// without a full blocking collection).
/// </para>
/// </remarks>
public interface IProcessRuntimeMetrics
{
    /// <summary>Managed heap size in bytes (same semantics as <see cref="GC.GetTotalMemory(bool)"/> with <c>false</c>).</summary>
    long ManagedMemoryBytes { get; }

    /// <summary><see cref="GC.CollectionCount(int)"/> for generation 0.</summary>
    int GcGen0Collections { get; }

    /// <summary><see cref="GC.CollectionCount(int)"/> for generation 1.</summary>
    int GcGen1Collections { get; }

    /// <summary><see cref="GC.CollectionCount(int)"/> for generation 2.</summary>
    int GcGen2Collections { get; }
}

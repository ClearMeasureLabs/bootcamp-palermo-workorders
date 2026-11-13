using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>Append-only audit storage and bounded historical lookup.</summary>
public interface IAuditLog
{
    /// <summary>Persists an operation using the current audit context.</summary>
    Task WriteAsync(string action, string entityType, Guid? entityId = null,
        string? previousValue = null, string? newValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>Reads at most 200 records, newest first, optionally before a sequence cursor.</summary>
    Task<AuditLog[]> ReadAsync(string? entityType, Guid? entityId, DateTime? from,
        long? beforeId, CancellationToken cancellationToken = default);

    /// <summary>Deletes only records older than the UTC retention cutoff.</summary>
    Task<int> PurgeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}

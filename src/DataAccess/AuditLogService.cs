using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess;

/// <summary>Append-only persistence with bounded keyset reads and explicit retention.</summary>
public sealed class AuditLogService(DbContext context) : IAuditLog
{
    /// <inheritdoc />
    public async Task WriteAsync(string action, string entityType, Guid? entityId = null,
        string? previousValue = null, string? newValue = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        var record = AuditLogFactory.Create(action, entityType, entityId, previousValue, newValue,
            entityType == nameof(WorkOrder) ? entityId : null);
        context.Add(record);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            context.Entry(record).State = EntityState.Detached;
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AuditLog[]> ReadAsync(string? entityType, Guid? entityId, DateTime? from,
        long? beforeId, CancellationToken cancellationToken = default)
    {
        var query = context.Set<AuditLog>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(log => log.EntityType == entityType);
        if (entityId.HasValue)
            query = query.Where(log => log.EntityId == entityId);
        if (from.HasValue)
            query = query.Where(log => log.Timestamp >= from);
        if (beforeId.HasValue)
            query = query.Where(log => log.Id < beforeId);
        return await query.OrderByDescending(log => log.Id).Take(200).ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> PurgeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        if (cutoffUtc.Kind != DateTimeKind.Utc || cutoffUtc > DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(cutoffUtc), "Retention requires a past UTC cutoff.");

        return await context.Set<AuditLog>().Where(log => log.Timestamp < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class DataContext : DbContext
{
    private readonly IDatabaseConfiguration _config;
    private readonly Dictionary<object, Dictionary<string, object?>> _unacceptedSnapshots =
        new(ReferenceEqualityComparer.Instance);

    public DataContext(IDatabaseConfiguration config, ILogger<DataContext>? logger = null)
    {
        _config = config;
        logger?.LogDebug(ToString());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _config.GetConnectionString();
        if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlite(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new WorkOrderMap().Map(modelBuilder);
        new EmployeeMap().Map(modelBuilder);
        new RoleMap().Map(modelBuilder);
        new WorkOrderAttachmentMap().Map(modelBuilder);
        new AuditLogMap().Map(modelBuilder);
        if (Database.IsSqlite())
            modelBuilder.Entity<AuditLog>().Property(log => log.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }

    /// <summary>Appends change snapshots in the same EF transaction as the business changes.</summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        LoadPersistedOriginals();
        LoadCascadeDependents();
        var logs = AppendAuditLogs();
        try
        {
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            CompleteAuditSave(logs, acceptAllChangesOnSuccess);
            return result;
        }
        catch
        {
            DetachGeneratedLogs(logs);
            throw;
        }
    }

    /// <summary>Appends change snapshots in the same EF transaction as the business changes.</summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await LoadPersistedOriginalsAsync(cancellationToken);
        await LoadCascadeDependentsAsync(cancellationToken);
        var logs = AppendAuditLogs();
        try
        {
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            CompleteAuditSave(logs, acceptAllChangesOnSuccess);
            return result;
        }
        catch
        {
            DetachGeneratedLogs(logs);
            throw;
        }
    }

    private AuditLog[] AppendAuditLogs()
    {
        ChangeTracker.DetectChanges();
        ChangeTracker.CascadeChanges();
        var entries = ChangeTracker.Entries().ToArray();
        foreach (var entry in entries.Where(entry => entry.State is EntityState.Unchanged or EntityState.Detached))
            _unacceptedSnapshots.Remove(entry.Entity);
        if (entries.Any(entry => entry.Entity is AuditLog &&
                entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit records are append-only. Use IAuditLog.PurgeAsync for retention.");

        var logs = entries.Where(IsAuditedChange).Select(CreateSnapshot)
            .Where(log => log != null).Cast<AuditLog>().ToArray();
        AddRange(logs);
        return logs;
    }

    private static bool IsAuditedChange(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
        (entry.Entity is WorkOrder or WorkOrderAttachment or Employee or Role ||
         entry.Metadata.Name == "EmployeeRoles");

    private AuditLog? CreateSnapshot(EntityEntry entry)
    {
        var properties = ChangedProperties(entry);
        if (properties.Length == 0)
            return null;

        return AuditLogFactory.Create(AuditContext.Current.Action ?? OperationName(entry.State),
            EntityTypeName(entry), EntityId(entry),
            SnapshotForState(entry, properties, true), SnapshotForState(entry, properties, false),
            RelatedWorkOrderId(entry.Entity));
    }

    private PropertyEntry[] ChangedProperties(EntityEntry entry) =>
        entry.Properties.Where(property => entry.State != EntityState.Modified ||
            property.IsModified && !Equals(OriginalValue(entry, property), property.CurrentValue)).ToArray();

    private string? SnapshotForState(EntityEntry entry, PropertyEntry[] properties, bool original)
    {
        var absentState = original ? EntityState.Added : EntityState.Deleted;
        return entry.State == absentState ? null : Snapshot(entry, properties, original);
    }

    private static string EntityTypeName(EntityEntry entry) =>
        entry.Metadata.ClrType == typeof(Dictionary<string, object>)
            ? entry.Metadata.Name : entry.Metadata.ClrType.Name;

    private static Guid? EntityId(EntityEntry entry) =>
        entry.Metadata.Name == "EmployeeRoles"
            ? (Guid?)entry.Property("EmployeeId").CurrentValue
            : (Guid?)entry.Property("Id").CurrentValue;

    private static Guid? RelatedWorkOrderId(object entity) => entity switch
        {
            WorkOrder order => order.Id,
            WorkOrderAttachment attachment => (Guid?)attachment.WorkOrderId,
            _ => null
        };

    private static string OperationName(EntityState state) => state switch
        {
            EntityState.Added => "Created",
            EntityState.Deleted => "Deleted",
            _ => "Modified"
        };

    private string Snapshot(EntityEntry entry, IEnumerable<PropertyEntry> properties, bool original) =>
        JsonSerializer.Serialize(properties.ToDictionary(property => property.Metadata.Name, property =>
        {
            var value = original ? OriginalValue(entry, property) : property.CurrentValue;
            var converter = property.Metadata.GetTypeMapping().Converter;
            return converter == null ? value : converter.ConvertToProvider(value);
        }));

    private object? OriginalValue(EntityEntry entry, PropertyEntry property) =>
        _unacceptedSnapshots.TryGetValue(entry.Entity, out var saved)
            ? saved[property.Metadata.Name]
            : property.OriginalValue;

    private void CompleteAuditSave(IEnumerable<AuditLog> logs, bool acceptAllChangesOnSuccess)
    {
        // Generated history must never be replayed when callers defer AcceptAllChanges.
        DetachGeneratedLogs(logs);
        _unacceptedSnapshots.Clear();
        if (acceptAllChangesOnSuccess)
            return;

        foreach (var entry in ChangeTracker.Entries().Where(IsAuditedChange))
            _unacceptedSnapshots[entry.Entity] = entry.Properties.ToDictionary(
                property => property.Metadata.Name, property => property.CurrentValue);
    }

    private void DetachGeneratedLogs(IEnumerable<AuditLog> logs)
    {
        foreach (var log in logs)
            Entry(log).State = EntityState.Detached;
    }

    private EntityEntry[] EntriesNeedingPersistedOriginals() => ChangeTracker.Entries()
        .Where(entry => entry.State == EntityState.Modified && IsAuditedChange(entry))
        .Where(RequiresOriginalRefresh).ToArray();

    private bool RequiresOriginalRefresh(EntityEntry entry)
    {
        var properties = entry.Properties.Where(property => !property.Metadata.IsPrimaryKey()).ToArray();
        return _unacceptedSnapshots.ContainsKey(entry.Entity) ||
               properties.All(property => property.IsModified) ||
               properties.All(property => Equals(property.OriginalValue, property.CurrentValue));
    }

    // Detached Update graphs have no database originals in EF's initial tracking snapshot.
    private void LoadPersistedOriginals()
    {
        foreach (var entry in EntriesNeedingPersistedOriginals())
        {
            var persisted = entry.GetDatabaseValues();
            if (persisted != null)
                StoreAuditOriginals(entry, persisted);
        }
    }

    private async Task LoadPersistedOriginalsAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in EntriesNeedingPersistedOriginals())
        {
            var persisted = await entry.GetDatabaseValuesAsync(cancellationToken);
            if (persisted != null)
                StoreAuditOriginals(entry, persisted);
        }
    }

    private void StoreAuditOriginals(EntityEntry entry, PropertyValues persisted) =>
        _unacceptedSnapshots[entry.Entity] = persisted.Properties.ToDictionary(
            property => property.Name, property => persisted[property.Name]);

    // Load database-cascaded rows before deletion so their snapshots are not silently lost.
    private void LoadCascadeDependents()
    {
        foreach (var entry in DeletedPrincipals())
        {
            if (entry.Entity is WorkOrder order)
                Set<WorkOrderAttachment>().Where(attachment => attachment.WorkOrderId == order.Id).Load();
            else
                Memberships(entry).Load();
        }
    }

    private async Task LoadCascadeDependentsAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in DeletedPrincipals())
        {
            if (entry.Entity is WorkOrder order)
                await Set<WorkOrderAttachment>().Where(attachment => attachment.WorkOrderId == order.Id)
                    .LoadAsync(cancellationToken);
            else
                await Memberships(entry).LoadAsync(cancellationToken);
        }
    }

    private EntityEntry[] DeletedPrincipals() => ChangeTracker.Entries()
        .Where(entry => entry.State == EntityState.Deleted && entry.Entity is WorkOrder or Employee or Role)
        .ToArray();

    private IQueryable<Dictionary<string, object>> Memberships(EntityEntry entry)
    {
        var key = entry.Entity is Employee ? "EmployeeId" : "RoleId";
        var id = (Guid)entry.Property("Id").CurrentValue!;
        return Set<Dictionary<string, object>>("EmployeeRoles")
            .Where(link => EF.Property<Guid>(link, key) == id);
    }

    public sealed override string ToString()
    {
        return base.ToString() + "-" + GetHashCode();
    }
}
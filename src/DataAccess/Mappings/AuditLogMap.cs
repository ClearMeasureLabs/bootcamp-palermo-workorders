using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>Maps historical references without foreign keys so deleted entities retain their history.</summary>
public sealed class AuditLogMap : IEntityFrameworkMapping
{
    /// <inheritdoc />
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog", "dbo", table => table.UseSqlOutputClause(false));
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).ValueGeneratedOnAdd();
            entity.Property(log => log.Action).IsRequired().HasMaxLength(255);
            entity.Property(log => log.EntityType).IsRequired().HasMaxLength(255);
            entity.Property(log => log.ActorUserName).IsRequired().HasMaxLength(255);
            entity.Property(log => log.ActorDisplayName).HasMaxLength(500);
            entity.Property(log => log.IpAddress).HasMaxLength(45);
            entity.Property(log => log.UserAgent).HasMaxLength(500);
            entity.Property(log => log.CorrelationId).HasMaxLength(64);
            entity.Property(log => log.Timestamp).HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            entity.HasIndex(log => log.Action);
            entity.HasIndex(log => new { log.EntityType, log.EntityId, log.Id });
            entity.HasIndex(log => new { log.WorkOrderId, log.Id });
            entity.HasIndex(log => new { log.Timestamp, log.Id }).IsDescending(true, true);
        });
    }
}

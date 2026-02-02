using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class AuditEntryMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntry", "dbo");
            entity.HasKey(e => new { e.WorkOrderId, e.Sequence });
            
            entity.Property(e => e.WorkOrderId).IsRequired();
            entity.Property(e => e.Sequence).IsRequired();
            entity.Property(e => e.EmployeeId);
            entity.Property(e => e.ArchivedEmployeeName).HasMaxLength(100);
            entity.Property(e => e.Date).IsRequired();
            
            entity.Property(e => e.BeginStatus)
                .HasConversion(
                    v => v == null ? null : v.Code,
                    v => v == null ? null : WorkOrderStatus.FromCode(v))
                .HasMaxLength(3);
                
            entity.Property(e => e.EndStatus)
                .HasConversion(
                    v => v == null ? null : v.Code,
                    v => v == null ? null : WorkOrderStatus.FromCode(v))
                .HasMaxLength(3);
        });
    }
}

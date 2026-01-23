using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class AuditEntryMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        var nullableStatusConverter = new NullableWorkOrderStatusConverter();

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntry", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(e => e.WorkOrderId).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.EmployeeName).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(50);

            entity.Property(e => e.BeginStatus)
                .HasConversion(nullableStatusConverter)
                .HasMaxLength(3);

            entity.Property(e => e.EndStatus)
                .HasConversion(nullableStatusConverter)
                .HasMaxLength(3);

            entity.HasOne(e => e.WorkOrder)
                .WithMany(w => w.AuditEntries)
                .HasForeignKey(e => e.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

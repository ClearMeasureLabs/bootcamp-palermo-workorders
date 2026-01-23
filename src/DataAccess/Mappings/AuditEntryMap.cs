using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class AuditEntryMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        var statusConverter = new WorkOrderStatusConverter();

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntry", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(e => e.WorkOrderId).IsRequired();
            entity.Property(e => e.EmployeeId);
            entity.Property(e => e.ArchivedEmployeeName).HasMaxLength(200);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50);

            // Configure Status with converter
            entity.Property(e => e.BeginStatus)
                .HasConversion(statusConverter)
                .HasMaxLength(3);
            entity.Property(e => e.EndStatus)
                .HasConversion(statusConverter)
                .HasMaxLength(3);

            // Configure relationships
            entity.HasOne(e => e.WorkOrder)
                .WithMany()
                .HasForeignKey(e => e.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

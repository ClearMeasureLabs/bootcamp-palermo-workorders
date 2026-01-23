using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderAuditEntryMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        var statusConverter = new WorkOrderStatusConverter();

        modelBuilder.Entity<WorkOrderAuditEntry>(entity =>
        {
            entity.ToTable("WorkOrderAuditEntry", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(e => e.ArchivedEmployeeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ActionDetails).HasMaxLength(500);

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
                .HasForeignKey("WorkOrderId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Restrict);

            // Configure navigation properties for eager loading
            entity.Navigation(e => e.WorkOrder).AutoInclude();
            entity.Navigation(e => e.Employee).AutoInclude();
        });
    }
}

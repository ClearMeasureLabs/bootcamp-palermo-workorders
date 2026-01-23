using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderAuditEntryMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        var nullableStatusConverter = new ValueConverter<WorkOrderStatus?, string?>(
            v => v == null ? null : v.Code,
            v => v == null ? null : WorkOrderStatus.FromCode(v));

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

            // Configure Status with converter for nullable WorkOrderStatus
            entity.Property(e => e.BeginStatus)
                .HasConversion(nullableStatusConverter)
                .HasMaxLength(3);

            entity.Property(e => e.EndStatus)
                .HasConversion(nullableStatusConverter)
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

using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class AuditEntryMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        var nullableStatusConverter = new ValueConverter<WorkOrderStatus?, string?>(
            v => v == null ? null : v.Code,
            v => v == null ? null : WorkOrderStatus.FromCode(v));

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntry", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(e => e.ArchivedEmployeeName).HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Date).IsRequired();

            // Configure Status with converter
            entity.Property(e => e.BeginStatus)
                .HasConversion(nullableStatusConverter)
                .HasMaxLength(3)
                .HasColumnName("BeginStatus");

            entity.Property(e => e.EndStatus)
                .HasConversion(nullableStatusConverter)
                .HasMaxLength(3)
                .HasColumnName("EndStatus");

            // Configure relationships
            entity.HasOne(e => e.WorkOrder)
                .WithMany(w => w.AuditEntries)
                .HasForeignKey("WorkOrderId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

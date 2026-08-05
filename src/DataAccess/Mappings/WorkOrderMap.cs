using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        var statusConverter = new WorkOrderStatusConverter();

        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.ToTable("WorkOrder", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(e => e.Number).IsRequired().HasMaxLength(7);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.Instructions).HasMaxLength(4000);
            entity.Property(e => e.RoomNumber).HasMaxLength(50);

            // Configure relationships
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey("CreatorId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Assignee)
                .WithMany()
                .HasForeignKey("AssigneeId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure navigation properties for eager loading
            entity.Navigation(e => e.Creator).AutoInclude();
            entity.Navigation(e => e.Assignee).AutoInclude();

            // Configure Status with converter
            entity.Property(e => e.Status)
                .HasConversion(statusConverter)
                .HasMaxLength(3);

            // Configure Priority
            entity.Property(e => e.Priority)
                .IsRequired()
                .HasDefaultValue(WorkOrderPriority.Normal);

            // Create index for priority-based queries
            entity.HasIndex(e => new { e.Priority, e.CreatedDate })
                .HasDatabaseName("IX_WorkOrders_Priority_CreatedDate");

            // Add check constraint for priority values
            entity.HasCheckConstraint("CK_WorkOrders_Priority", "Priority IN (0, 1, 2, 3)");

            // Configure Recurrence properties
            entity.Property(e => e.IsRecurring)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.RecurrencePattern)
                .IsRequired()
                .HasDefaultValue(RecurrencePattern.None);

            entity.Property(e => e.RecurrenceInterval)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.NextScheduledDate)
                .IsRequired(false);

            entity.Property(e => e.ParentWorkOrderId)
                .IsRequired(false);

            // Self-referencing relationship for parent-child work orders
            entity.HasOne(e => e.ParentWorkOrder)
                .WithMany(e => e.ChildWorkOrders)
                .HasForeignKey(e => e.ParentWorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for recurring work order queries
            entity.HasIndex(e => new { e.IsRecurring, e.NextScheduledDate })
                .HasDatabaseName("IX_WorkOrders_Recurring")
                .HasFilter("IsRecurring = 1");

            // Index for parent-child lookups
            entity.HasIndex(e => e.ParentWorkOrderId)
                .HasDatabaseName("IX_WorkOrders_ParentId")
                .HasFilter("ParentWorkOrderId IS NOT NULL");

            // Check constraint for recurrence interval
            entity.HasCheckConstraint("CK_WorkOrders_RecurrenceInterval", "RecurrenceInterval >= 1");
        });
    }
}
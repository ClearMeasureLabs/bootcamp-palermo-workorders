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
            entity.Property(e => e.Title).IsRequired().HasMaxLength(700);
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.RoomNumber).HasMaxLength(50);

            // Configure relationships
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey("CreatorId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Assignee)
                .WithMany()
                .HasForeignKey("AssigneeId")
                .OnDelete(DeleteBehavior.Restrict);

            // Configure navigation properties for eager loading
            entity.Navigation(e => e.Creator).AutoInclude();
            entity.Navigation(e => e.Assignee).AutoInclude();

            // Configure Status with converter
            entity.Property(e => e.Status)
                .HasConversion(statusConverter)
                .HasMaxLength(3);
        });
    }
}
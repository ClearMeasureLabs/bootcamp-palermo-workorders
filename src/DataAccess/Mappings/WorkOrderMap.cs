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
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(4000);

            // Configure relationships
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey("CreatorId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Assignee)
                .WithMany()
                .HasForeignKey("AssigneeId")
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Rooms many-to-many relationship
            entity.HasMany(w => w.Rooms)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "WorkOrderRooms",
                    r => r.HasOne<Room>().WithMany().HasForeignKey("RoomId").OnDelete(DeleteBehavior.Cascade),
                    l => l.HasOne<WorkOrder>().WithMany().HasForeignKey("WorkOrderId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("WorkOrderId", "RoomId");
                        j.ToTable("WorkOrderRooms", "dbo");
                    });

            // Configure navigation properties for eager loading
            entity.Navigation(e => e.Creator).AutoInclude();
            entity.Navigation(e => e.Assignee).AutoInclude();
            entity.Navigation(e => e.Rooms).AutoInclude();

            // Configure Status with converter
            entity.Property(e => e.Status)
                .HasConversion(statusConverter)
                .HasMaxLength(3);
        });
    }
}
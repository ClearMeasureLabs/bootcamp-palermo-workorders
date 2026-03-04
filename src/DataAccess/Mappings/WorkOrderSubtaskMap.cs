using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderSubtaskMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderSubtask>(entity =>
        {
            entity.ToTable("WorkOrderSubtask", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.IsCompleted).IsRequired();
            entity.Property(e => e.SortOrder).IsRequired();
            entity.Property(e => e.WorkOrderId).IsRequired();
        });

        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasMany(e => e.Subtasks)
                .WithOne()
                .HasForeignKey(s => s.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(e => e.Subtasks).AutoInclude();
        });
    }
}

using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderNoteMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderNote>(entity =>
        {
            entity.ToTable("WorkOrderNote", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired().ValueGeneratedNever();
            entity.Property(e => e.WorkOrderId).IsRequired();
            entity.Property(e => e.AuthorId).IsRequired();
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne<WorkOrder>()
                .WithMany()
                .HasForeignKey(e => e.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

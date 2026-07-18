using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderAttachmentMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderAttachment>(entity =>
        {
            entity.ToTable("WorkOrderAttachment", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired().ValueGeneratedNever();
            entity.Property(e => e.WorkOrderId).IsRequired();
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.UploadedById).IsRequired();
            entity.Property(e => e.UploadedDate).IsRequired();

            entity.HasOne<WorkOrder>()
                .WithMany(w => w.Attachments)
                .HasForeignKey(e => e.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

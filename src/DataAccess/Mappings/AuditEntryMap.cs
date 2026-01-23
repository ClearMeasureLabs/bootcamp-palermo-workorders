using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class AuditEntryMap : IEntityFrameworkMapping
{
	public void Map(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<AuditEntry>(entity =>
		{
			entity.ToTable("AuditEntry", "dbo");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).IsRequired()
				.ValueGeneratedOnAdd()
				.HasDefaultValue(Guid.Empty);

			entity.Property(e => e.WorkOrderId).IsRequired();
			entity.Property(e => e.UserName).HasMaxLength(100);
			entity.Property(e => e.Timestamp).IsRequired();
			entity.Property(e => e.Action).HasMaxLength(50);
			entity.Property(e => e.OldStatus).HasMaxLength(50);
			entity.Property(e => e.NewStatus).HasMaxLength(50);
			entity.Property(e => e.Details).HasMaxLength(500);

			// Configure relationship
			entity.HasOne(e => e.WorkOrder)
				.WithMany()
				.HasForeignKey(e => e.WorkOrderId)
				.OnDelete(DeleteBehavior.Restrict);
		});
	}
}

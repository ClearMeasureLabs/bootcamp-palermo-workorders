using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class RoleMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            // Configure properties
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CanCreateWorkOrder).IsRequired();
            entity.Property(e => e.CanFulfillWorkOrder).IsRequired();
        });
    }
}
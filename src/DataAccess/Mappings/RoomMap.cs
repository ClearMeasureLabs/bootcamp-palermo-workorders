using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class RoomMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Room", "dbo");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).IsRequired()
                .ValueGeneratedOnAdd();

            entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
        });
    }
}

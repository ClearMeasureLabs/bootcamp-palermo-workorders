using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class EmployeeMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            // Configure properties
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(255);
            MapPhoneNumber(entity);
            entity.Property(e => e.PreferredLanguage).IsRequired().HasMaxLength(10).HasDefaultValue("en-US");

            // Configure Roles collection
            entity.HasMany(e => e.Roles)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "EmployeeRoles",
                    r => r.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<Employee>().WithMany().HasForeignKey("EmployeeId"),
                    j =>
                    {
                        j.HasKey("EmployeeId", "RoleId");
                        j.ToTable("EmployeeRoles", "dbo");
                    });
        });
    }

    private static void MapPhoneNumber(EntityTypeBuilder<Employee> entity)
    {
        entity.Property(e => e.PhoneNumber).HasMaxLength(30);
    }
}
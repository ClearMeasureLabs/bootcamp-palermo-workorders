using Microsoft.EntityFrameworkCore;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;

namespace DataAccess.Mappings
{
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
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(255);

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
    }
}
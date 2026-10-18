using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class RoleMappingTests
{
    [Test, Retry(2)]
    public void ShouldPersistRoles()
    {
        // Empty the database
        EmptyDatabase();

        // Create and save a Role
        var role = new Role("foo", true, true);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(role);
            context.SaveChanges();
        }

        // Retrieve the saved Role
        Role rehydratedRole;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedRole = context.Set<Role>()
                .Single(r => r.Id == role.Id);
        }

        // Assert
        rehydratedRole.Id.ShouldBe(role.Id);
        rehydratedRole.Name.ShouldBe("foo");
        rehydratedRole.CanCreateWorkOrder.ShouldBeTrue();
        rehydratedRole.CanFulfillWorkOrder.ShouldBeTrue();
    }

    [Test]
    public void ShouldPersistDescription()
    {
        EmptyDatabase();

        var role = new Role("descrole", true, false) { Description = "Short role description" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(role);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Role>().Single(r => r.Id == role.Id);
            rehydrated.Description.ShouldBe("Short role description");
        }
    }

    private void EmptyDatabase()
    {
        new DatabaseEmptier(TestHost.GetRequiredService<DbContext>().Database).DeleteAllData();
    }
}
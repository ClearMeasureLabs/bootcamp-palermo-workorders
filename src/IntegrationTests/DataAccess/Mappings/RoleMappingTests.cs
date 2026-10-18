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
        EmptyDatabase();

        var role = new Role("foo", true, true);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(role);
            context.SaveChanges();
        }

        Role rehydratedRole;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedRole = context.Set<Role>()
                .Single(r => r.Id == role.Id);
        }

        rehydratedRole.Id.ShouldBe(role.Id);
        rehydratedRole.Name.ShouldBe("foo");
        rehydratedRole.CanCreateWorkOrder.ShouldBeTrue();
        rehydratedRole.CanFulfillWorkOrder.ShouldBeTrue();
    }

    private void EmptyDatabase()
    {
        new DatabaseEmptier(TestHost.GetRequiredService<DbContext>().Database).DeleteAllData();
    }
}
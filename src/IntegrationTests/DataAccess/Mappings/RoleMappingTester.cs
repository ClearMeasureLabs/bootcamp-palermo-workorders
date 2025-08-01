using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ClearMeasure.Bootcamp.IntegrationTests;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings
{
    [TestFixture]
    public class RoleMappingTester
    {
        [Test]
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

        private void EmptyDatabase()
        {
            new DatabaseEmptier(TestHost.GetRequiredService<DbContext>().Database).DeleteAllData();
        }
    }
}
using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

public class EmployeeMappingTests
{
    [Test]
    public void ShouldSaveRolesWithEmployee()
    {
        new DatabaseTester().Clean();

        var role1 = new Role("foo", false, false);
        var role2 = new Role("bar", true, true);
        var emp1 = new Employee("1", "first1", "last1", "email1");
        emp1.AddRole(role1);
        emp1.AddRole(role2);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(role1);
            context.Add(role2);
            context.Add(emp1);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedEmployee = context.Set<Employee>()
                .Include("Roles")
                .Single(e => e.Id == emp1.Id);

            Assert.That(rehydratedEmployee.Roles.Count, Is.EqualTo(2));
        }
    }
}
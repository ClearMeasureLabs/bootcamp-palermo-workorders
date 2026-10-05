using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

public class EmployeeMappingTests
{
    [Test]
    public void ShouldSaveRolesWithEmployee()
    {
        new DatabaseTests().Clean();

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

    [Test]
    public void ShouldPersistLastNameUpTo120Characters()
    {
        new DatabaseTests().Clean();

        var longLastName = new string('A', 120);
        var emp = new Employee("user120", "First", longLastName, "email120@example.com");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(emp);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Employee>().Single(e => e.Id == emp.Id);
            Assert.That(rehydrated.LastName, Is.EqualTo(longLastName));
        }
    }

    [Test]
    public void ShouldPersistMiddleName()
    {
        new DatabaseTests().Clean();

        var emp = new Employee("usermiddle", "First", "Last", "emailmiddle@example.com") { MiddleName = "Middle" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(emp);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Employee>().Single(e => e.Id == emp.Id);
            rehydrated.MiddleName.ShouldBe("Middle");
        }
    }

    [Test]
    public void ShouldPersistNullMiddleName()
    {
        new DatabaseTests().Clean();

        var emp = new Employee("usernomiddle", "First", "Last", "emailnomiddle@example.com") { MiddleName = null };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(emp);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Employee>().Single(e => e.Id == emp.Id);
            rehydrated.MiddleName.ShouldBeNull();
        }
    }

    [Test]
    public void ShouldPersistMiddleNameUpTo100Characters()
    {
        new DatabaseTests().Clean();

        var longMiddleName = new string('M', 100);
        var emp = new Employee("user100", "First", "Last", "email100@example.com") { MiddleName = longMiddleName };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(emp);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Employee>().Single(e => e.Id == emp.Id);
            rehydrated.MiddleName.ShouldBe(longMiddleName);
        }
    }
}
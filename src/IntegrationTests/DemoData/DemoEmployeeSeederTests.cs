using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DemoData;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DemoData;

[TestFixture]
public class DemoEmployeeSeederTests
{
    [Test]
    public void ShouldAddEveryDemoRoleEmployeeAndLink_WhenDatabaseIsEmpty()
    {
        new DatabaseTests().Clean();

        var result = Seed();

        result.RolesAdded.ShouldBe(10);
        result.RolesFound.ShouldBe(0);
        result.EmployeesAdded.ShouldBe(28);
        result.EmployeesFound.ShouldBe(0);
        result.RoleLinksAdded.ShouldBe(40);
        using var context = TestHost.GetRequiredService<DbContext>();
        var lovejoy = context.Set<Employee>().Include(e => e.Roles).Single(e => e.UserName == "tlovejoy");
        lovejoy.GetFullName().ShouldBe("Timothy Lovejoy Jr");
        lovejoy.EmailAddress.ShouldBe("reverend@firstchurchspringfield.org");
        lovejoy.PreferredLanguage.ShouldBe("en-US");
        lovejoy.Roles.Single().Name.ShouldBe("Minister");
        lovejoy.CanCreateWorkOrder().ShouldBeTrue();
        lovejoy.CanFulfillWorkOrder().ShouldBeTrue();
        context.Set<Employee>().Single(e => e.UserName == "jcuevas").PreferredLanguage.ShouldBe("es-ES");
        var fmanager = context.Set<Employee>().Include(e => e.Roles).Single(e => e.UserName == "fmanager");
        fmanager.Roles.Single().Name.ShouldBe("Facilities Manager");
        fmanager.CanCreateWorkOrder().ShouldBeTrue();
        fmanager.CanFulfillWorkOrder().ShouldBeTrue();
    }

    [Test]
    public void ShouldAddNothing_WhenRunAgain()
    {
        new DatabaseTests().Clean();
        Seed();

        var result = Seed();

        result.ShouldBe(new DemoEmployeeSeedResult(0, 10, 0, 28, 0));
        using var context = TestHost.GetRequiredService<DbContext>();
        context.Set<Employee>().Count().ShouldBe(28);
        context.Set<Role>().Count().ShouldBe(10);
        context.Set<Employee>().Count(e => e.UserName == "fmanager").ShouldBe(1);
    }

    [Test]
    public void ShouldKeepExistingEmployeeAndAddOnlyMissingRoles_WhenUserNameExists()
    {
        new DatabaseTests().Clean();
        var deacon = new Role("Deacon", true, true);
        var existing = new Employee("nflanders", "Nedward", "Flanders", "ned@leftorium.com")
        {
            PreferredLanguage = "fr-FR"
        };
        existing.AddRole(deacon);
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(deacon);
            context.Add(existing);
            context.SaveChanges();
        }

        var result = Seed();

        result.RolesAdded.ShouldBe(9);
        result.RolesFound.ShouldBe(1);
        result.EmployeesAdded.ShouldBe(27);
        result.EmployeesFound.ShouldBe(1);
        result.RoleLinksAdded.ShouldBe(39);
        using var verify = TestHost.GetRequiredService<DbContext>();
        var ned = verify.Set<Employee>().Include(e => e.Roles).Single(e => e.UserName == "nflanders");
        ned.Id.ShouldBe(existing.Id);
        ned.FirstName.ShouldBe("Nedward");
        ned.EmailAddress.ShouldBe("ned@leftorium.com");
        ned.PreferredLanguage.ShouldBe("fr-FR");
        string.Join(", ", ned.Roles.Select(r => r.Name).Order()).ShouldBe("Deacon, Parishioner");
        var keptDeacon = verify.Set<Role>().Single(r => r.Name == "Deacon");
        keptDeacon.Id.ShouldBe(deacon.Id);
        keptDeacon.CanCreateWorkOrder.ShouldBeTrue();
    }

    private static DemoEmployeeSeedResult Seed()
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        return DemoEmployeeSeeder.Seed(context);
    }
}

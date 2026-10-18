using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpEmployeeToolTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldListAllEmployees()
    {
        var emp1 = new Employee("jdoe", "John", "Doe", "john@test.com");
        var emp2 = new Employee("jsmith", "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp1);
        context.Add(emp2);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.ListEmployees(bus);

        result.ShouldContain("jdoe");
        result.ShouldContain("jsmith");
        result.ShouldContain("John");
        result.ShouldContain("Jane");
    }

    [Test]
    public async Task ShouldReturnEmptyListWhenNoEmployees()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.ListEmployees(bus);

        result.ShouldContain("[]");
    }

    [Test]
    public async Task ShouldGetEmployeeByUsername()
    {
        var emp = new Employee("jdoe", "John", "Doe", "john@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.GetEmployee(bus, "jdoe");

        result.ShouldContain("jdoe");
        result.ShouldContain("John");
        result.ShouldContain("Doe");
        result.ShouldContain("john@test.com");
    }

    [Test]
    public async Task ShouldIncludeMiddleName_WhenGettingEmployeeByUsername()
    {
        var emp = new Employee("mjdoe", "Mary", "Doe", "mary@test.com") { MiddleName = "Jane" };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.GetEmployee(bus, "mjdoe");

        result.ShouldContain("\"MiddleName\": \"Jane\"");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingEmployee()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.GetEmployee(bus, "nonexistent");

        result.ShouldContain("No employee found");
    }

    [Test]
    public async Task ShouldFilterEmployeesByRole()
    {
        var adminRole = new Role("Admin", true, true);
        var fulfillRole = new Role("Fulfiller", false, true);
        var admin = new Employee("admin", "Ada", "Admin", "ada@test.com");
        admin.Roles.Add(adminRole);
        var fulfiller = new Employee("fulfiller", "Fred", "Fulfill", "fred@test.com");
        fulfiller.Roles.Add(fulfillRole);

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(adminRole);
        context.Add(fulfillRole);
        context.Add(admin);
        context.Add(fulfiller);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.ListEmployees(bus, "admin");

        result.ShouldContain("admin");
        result.ShouldNotContain("fulfiller");
    }

    [Test]
    public async Task ShouldListAllEmployeesWhenRoleBlank()
    {
        var emp1 = new Employee("jdoe", "John", "Doe", "john@test.com");
        var emp2 = new Employee("jsmith", "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp1);
        context.Add(emp2);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.ListEmployees(bus, "   ");

        result.ShouldContain("jdoe");
        result.ShouldContain("jsmith");
    }

    [Test]
    public async Task ShouldReturnEmptyListForUnknownRole()
    {
        var role = new Role("Admin", true, true);
        var emp = new Employee("jdoe", "John", "Doe", "john@test.com");
        emp.Roles.Add(role);

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(role);
        context.Add(emp);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.ListEmployees(bus, "NonexistentRole");

        result.ShouldContain("[]");
    }
}

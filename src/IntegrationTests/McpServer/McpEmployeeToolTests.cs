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

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(emp1);
            context.Add(emp2);
            await context.SaveChangesAsync();
        }

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

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(emp);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.GetEmployee(bus, "jdoe");

        result.ShouldContain("jdoe");
        result.ShouldContain("John");
        result.ShouldContain("Doe");
        result.ShouldContain("john@test.com");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingEmployee()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.GetEmployee(bus, "nonexistent");

        result.ShouldContain("No employee found");
    }
}

using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class EmployeeQueryHandlerTests
{
    [Test]
    public async Task ShouldFindEmployeeByUsername()
    {
        new DatabaseTests().Clean();

        var one = new Employee("1", "first1", "last1", "email1");
        var two = new Employee("2", "first2", "last2", "email2");
        var three = new Employee("3", "first3", "last3", "email3");
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(one);
            context.Add(two);
            context.Add(three);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeQueryHandler(dataContext);
        var employee = await handler.Handle(new EmployeeByUserNameQuery("1"));
        Assert.That(employee.Id, Is.EqualTo(one.Id));
    }

    [Test]
    public async Task ShouldGetAllEmployees()
    {
        new DatabaseTests().Clean();

        var one = new Employee("1", "first1", "last1", "email1");
        var two = new Employee("2", "first2", "last2", "email2");
        var three = new Employee("3", "first3", "last3", "email3");
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(two);
            context.Add(three);
            context.Add(one);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeQueryHandler(dataContext);
        var employees = await handler.Handle(new EmployeeGetAllQuery());

        Assert.That(employees.Length, Is.EqualTo(3));
        Assert.That(employees[0].UserName, Is.EqualTo("1"));
        Assert.That(employees[0].FirstName, Is.EqualTo("first1"));
        Assert.That(employees[0].LastName, Is.EqualTo("last1"));
        Assert.That(employees[0].EmailAddress, Is.EqualTo("email1"));
    }

    [Test]
    public void ShouldPersistPreferredLanguage()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("testuser", "Test", "User", "test@test.com");
        employee.PreferredLanguage = "fr-FR";
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedEmployee = context.Set<Employee>().First(e => e.Id == employee.Id);
            rehydratedEmployee.PreferredLanguage.ShouldBe("fr-FR");
        }
    }

    [Test]
    public void ShouldPersistDefaultPreferredLanguage()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("testuser", "Test", "User", "test@test.com");
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedEmployee = context.Set<Employee>().First(e => e.Id == employee.Id);
            rehydratedEmployee.PreferredLanguage.ShouldBe("en-US");
        }
    }
}
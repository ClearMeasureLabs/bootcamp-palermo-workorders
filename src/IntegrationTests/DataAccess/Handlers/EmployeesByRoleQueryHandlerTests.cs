using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class EmployeesByRoleQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnEmployeesWithMatchingRole_CaseInsensitive()
    {
        new DatabaseTests().Clean();

        var managerRole = new Role("Manager", true, false);
        var workerRole = new Role("Worker", false, true);
        var alice = new Employee("alice", "Alice", "Anderson", "alice@test.com");
        alice.AddRole(managerRole);
        var bob = new Employee("bob", "Bob", "Brown", "bob@test.com");
        bob.AddRole(workerRole);
        var carol = new Employee("carol", "Carol", "Clark", "carol@test.com");
        carol.AddRole(managerRole);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(managerRole);
            context.Add(workerRole);
            context.Add(alice);
            context.Add(bob);
            context.Add(carol);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeesByRoleQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeesByRoleQuery("manager"));

        result.Length.ShouldBe(2);
        result.Select(e => e.UserName).ShouldBe(["alice", "carol"], ignoreOrder: true);
    }
}

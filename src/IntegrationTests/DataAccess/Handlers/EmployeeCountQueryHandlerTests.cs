using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class EmployeeCountQueryHandlerTests
{
    [Test]
    public async Task ShouldGrowByTwo_WhenTwoEmployeesAdded()
    {
        new DatabaseTests().Clean();

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeCountQueryHandler(dataContext);
        var before = await handler.Handle(new EmployeeCountQuery());

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(new Employee("ecq_user1", "First", "One", "one@test.com"));
            context.Add(new Employee("ecq_user2", "First", "Two", "two@test.com"));
            await context.SaveChangesAsync();
        }

        dataContext = TestHost.GetRequiredService<DataContext>();
        handler = new EmployeeCountQueryHandler(dataContext);
        var after = await handler.Handle(new EmployeeCountQuery());

        after.ShouldBe(before + 2);
    }
}

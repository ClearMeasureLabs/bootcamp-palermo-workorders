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
    public async Task ShouldReturnEmployeeCountFromDatabase()
    {
        new DatabaseTests().Clean();

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(new Employee("ecnt_1", "First", "One", "one@test.com"));
            context.Add(new Employee("ecnt_2", "Second", "Two", "two@test.com"));
            context.Add(new Employee("ecnt_3", "Third", "Three", "three@test.com"));
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeCountQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeCountQuery());

        result.ShouldBe(3);
    }
}

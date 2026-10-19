using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrdersCreatedInLastDaysQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnOnlyWorkOrdersInsideTheWindow_NewestFirst()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("wocild_u", "First", "Last", "email@test.com");
        var now = DateTime.UtcNow;
        var insideRecent = new WorkOrder
        {
            Creator = creator,
            Number = "WIC-1",
            Title = "Inside recent",
            CreatedDate = now.AddDays(-2)
        };
        var insideOlder = new WorkOrder
        {
            Creator = creator,
            Number = "WIC-2",
            Title = "Inside older",
            CreatedDate = now.AddDays(-5)
        };
        var outside = new WorkOrder
        {
            Creator = creator,
            Number = "WIC-3",
            Title = "Outside window",
            CreatedDate = now.AddDays(-10)
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(insideRecent);
            context.Add(insideOlder);
            context.Add(outside);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrdersCreatedInLastDaysQueryHandler(dataContext);
        var result = await handler.Handle(new WorkOrdersCreatedInLastDaysQuery(7));

        result.Length.ShouldBe(2);
        result[0].Number.ShouldBe("WIC-1");
        result[1].Number.ShouldBe("WIC-2");
        result.ShouldNotContain(wo => wo.Number == "WIC-3");
    }
}

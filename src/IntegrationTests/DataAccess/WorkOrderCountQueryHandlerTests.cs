using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderCountQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnTotalWorkOrderCount()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var order1 = new WorkOrder { Creator = creator, Number = "123" };
        var order2 = new WorkOrder { Creator = creator, Number = "456" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order1);
            context.Add(order2);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountQueryHandler(dataContext);

        var count = await handler.Handle(new WorkOrderCountQuery());

        count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldReturnZeroWhenNoWorkOrdersExist()
    {
        new DatabaseTests().Clean();

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountQueryHandler(dataContext);

        var count = await handler.Handle(new WorkOrderCountQuery());

        count.ShouldBe(0);
    }
}

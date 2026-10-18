using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class OverdueWorkOrderCountQueryHandlerTests
{
    [Test]
    public async Task ShouldCountOverdueWorkOrdersBeforeAsOfDate()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("owoc_user", "First", "Last", "email@test.com");
        var asOfDate = new DateOnly(2024, 6, 15);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOC-1",
                Status = WorkOrderStatus.Draft,
                DueDate = new DateOnly(2024, 1, 1)
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOC-2",
                Status = WorkOrderStatus.InProgress,
                DueDate = new DateOnly(2024, 6, 1)
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOC-3",
                Status = WorkOrderStatus.Complete,
                DueDate = new DateOnly(2024, 1, 1)
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOC-4",
                Status = WorkOrderStatus.Cancelled,
                DueDate = new DateOnly(2024, 1, 1)
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOC-5",
                Status = WorkOrderStatus.Assigned,
                DueDate = new DateOnly(2024, 12, 31)
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new OverdueWorkOrderCountQueryHandler(dataContext);
        var result = await handler.Handle(new OverdueWorkOrderCountQuery(asOfDate));

        result.ShouldBe(2);
    }
}

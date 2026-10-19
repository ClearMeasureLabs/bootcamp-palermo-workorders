using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrdersDueSoonQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnOpenWorkOrdersInsideDueWindow()
    {
        new DatabaseTests().Clean();

        var today = ChurchTimeZone.Today(TestHost.GetRequiredService<TimeProvider>());
        var creator = new Employee("dssoon_u", "First", "Last", "email@test.com");
        var inside = new WorkOrder
        {
            Creator = creator,
            Number = "DS-IN1",
            Status = WorkOrderStatus.Draft,
            DueDate = today.AddDays(3)
        };
        var pastDue = new WorkOrder
        {
            Creator = creator,
            Number = "DS-PAST",
            Status = WorkOrderStatus.Assigned,
            DueDate = today.AddDays(-1)
        };
        var beyond = new WorkOrder
        {
            Creator = creator,
            Number = "DS-BEY",
            Status = WorkOrderStatus.InProgress,
            DueDate = today.AddDays(8)
        };
        var closed = new WorkOrder
        {
            Creator = creator,
            Number = "DS-CLSD",
            Status = WorkOrderStatus.Complete,
            DueDate = today.AddDays(2)
        };
        var noDue = new WorkOrder
        {
            Creator = creator,
            Number = "DS-NODE",
            Status = WorkOrderStatus.Draft,
            DueDate = null
        };
        var boundaryEnd = new WorkOrder
        {
            Creator = creator,
            Number = "DS-END",
            Status = WorkOrderStatus.Assigned,
            DueDate = today.AddDays(7)
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.AddRange(inside, pastDue, beyond, closed, noDue, boundaryEnd);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var timeProvider = TestHost.GetRequiredService<TimeProvider>();
        var handler = new WorkOrdersDueSoonQueryHandler(dataContext, timeProvider);
        var result = await handler.Handle(new WorkOrdersDueSoonQuery(7));

        result.Select(wo => wo.Number).ShouldBe(["DS-IN1", "DS-END"]);
    }
}

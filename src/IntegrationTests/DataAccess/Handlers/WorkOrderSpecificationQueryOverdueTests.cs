using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderSpecificationQueryOverdueTests
{
    [Test]
    public async Task ShouldReturnOnlyOpenOverdueWorkOrdersOrderedByDueDate()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("woso_user", "First", "Last", "email@test.com");
        var oldest = new WorkOrder
        {
            Creator = creator,
            Number = "WOSO-1",
            Status = WorkOrderStatus.InProgress,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10))
        };
        var newer = new WorkOrder
        {
            Creator = creator,
            Number = "WOSO-2",
            Status = WorkOrderStatus.Assigned,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2))
        };
        var notOverdue = new WorkOrder
        {
            Creator = creator,
            Number = "WOSO-3",
            Status = WorkOrderStatus.Draft,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };
        var complete = new WorkOrder
        {
            Creator = creator,
            Number = "WOSO-4",
            Status = WorkOrderStatus.Complete,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3))
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(oldest);
            context.Add(newer);
            context.Add(notOverdue);
            context.Add(complete);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var query = new WorkOrderSpecificationQuery();
        query.MatchOverdueOnly(true);
        var result = await bus.Send(query);

        result.Select(wo => wo.Number).ShouldBe(["WOSO-1", "WOSO-2"]);
    }
}

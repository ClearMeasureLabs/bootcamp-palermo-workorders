using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class StaleWorkOrdersQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnOnlyStaleAssignedAndInProgressOldestFirst()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("stale_u", "First", "Last", "email@test.com");
        var assignee = new Employee("stale_a", "Ann", "Assignee", "ann@test.com");
        var now = DateTime.UtcNow;

        var staleAssigned = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "ST-001",
            Status = WorkOrderStatus.Assigned,
            AssignedDate = now.AddDays(-20)
        };
        var staleInProgress = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "ST-002",
            Status = WorkOrderStatus.InProgress,
            AssignedDate = now.AddDays(-30)
        };
        var recentAssigned = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "ST-003",
            Status = WorkOrderStatus.Assigned,
            AssignedDate = now.AddDays(-5)
        };
        var staleDraft = new WorkOrder
        {
            Creator = creator,
            Number = "ST-004",
            Status = WorkOrderStatus.Draft,
            AssignedDate = now.AddDays(-40)
        };
        var staleComplete = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "ST-005",
            Status = WorkOrderStatus.Complete,
            AssignedDate = now.AddDays(-40)
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(staleAssigned);
            context.Add(staleInProgress);
            context.Add(recentAssigned);
            context.Add(staleDraft);
            context.Add(staleComplete);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new StaleWorkOrdersQueryHandler(dataContext);
        var result = await handler.Handle(new StaleWorkOrdersQuery(14));

        result.Select(wo => wo.Number).ShouldBe(["ST-002", "ST-001"]);
    }

    [Test]
    public async Task ShouldExcludeOrdersInsideTheWindow()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("stale_w", "First", "Last", "email@test.com");
        var now = DateTime.UtcNow;
        var insideWindow = new WorkOrder
        {
            Creator = creator,
            Number = "ST-010",
            Status = WorkOrderStatus.Assigned,
            AssignedDate = now.AddDays(-3)
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(insideWindow);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new StaleWorkOrdersQueryHandler(dataContext);
        var result = await handler.Handle(new StaleWorkOrdersQuery(14));

        result.ShouldBeEmpty();
    }
}

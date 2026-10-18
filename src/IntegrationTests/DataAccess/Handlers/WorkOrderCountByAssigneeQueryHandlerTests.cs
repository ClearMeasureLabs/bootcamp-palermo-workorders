using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderCountByAssigneeQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnCountsForTwoAssignees()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("wocba_c", "First", "Last", "email@test.com");
        var assigneeA = new Employee("wocba_a", "Alice", "A", "a@test.com");
        var assigneeB = new Employee("wocba_b", "Bob", "B", "b@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assigneeA);
            context.Add(assigneeB);
            var order1 = new WorkOrder
            {
                Creator = creator, Assignee = assigneeA, Number = "WOCBA1", Status = WorkOrderStatus.Assigned
            };
            var order2 = new WorkOrder
            {
                Creator = creator, Assignee = assigneeA, Number = "WOCBA2", Status = WorkOrderStatus.Assigned
            };
            var order3 = new WorkOrder
            {
                Creator = creator, Assignee = assigneeB, Number = "WOCBA3", Status = WorkOrderStatus.Assigned
            };
            context.Add(order1);
            context.Add(order2);
            context.Add(order3);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountByAssigneeQueryHandler(dataContext);

        var countA = await handler.Handle(new WorkOrderCountByAssigneeQuery("wocba_a"));
        var countB = await handler.Handle(new WorkOrderCountByAssigneeQuery("wocba_b"));

        countA.ShouldBe(2);
        countB.ShouldBe(1);
    }
}

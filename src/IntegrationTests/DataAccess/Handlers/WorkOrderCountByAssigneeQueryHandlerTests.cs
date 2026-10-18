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
    public async Task ShouldReturnOpenCountsGroupedByAssignee()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("wocba_creator", "First", "Last", "email@test.com");
        var alice = new Employee("wocba_alice", "Alice", "A", "alice@test.com");
        var bob = new Employee("wocba_bob", "Bob", "B", "bob@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(alice);
            context.Add(bob);
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = alice,
                Number = "WOCBA1",
                Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = alice,
                Number = "WOCBA2",
                Status = WorkOrderStatus.InProgress
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = bob,
                Number = "WOCBA3",
                Status = WorkOrderStatus.Draft
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = bob,
                Number = "WOCBA4",
                Status = WorkOrderStatus.Complete
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountByAssigneeQueryHandler(dataContext);
        var result = await handler.Handle(new WorkOrderCountByAssigneeQuery());

        result["wocba_alice"].ShouldBe(2);
        result["wocba_bob"].ShouldBe(1);
        result.ContainsKey("wocba_creator").ShouldBeFalse();
    }
}

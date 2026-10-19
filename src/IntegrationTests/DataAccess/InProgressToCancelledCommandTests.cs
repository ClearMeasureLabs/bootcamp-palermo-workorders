using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class InProgressToCancelledCommandTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldPersistAbandonThroughStateCommandHandler()
    {
        var creator = new Employee("creator1", "Jane", "Creator", "jane@test.com");
        var assignee = new Employee("assignee1", "Bob", "Worker", "bob@test.com");
        var order = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "WO-AB1",
            Title = "In progress job",
            Status = WorkOrderStatus.InProgress,
            AssignedDate = DateTime.UtcNow
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var orderId = order.Id;
        var bus = TestHost.GetRequiredService<IBus>();
        await bus.Send(new InProgressToCancelledCommand(order, creator));

        await using var verifyContext = TestHost.GetRequiredService<DbContext>();
        var persisted = await verifyContext.Set<WorkOrder>()
            .Include(wo => wo.Assignee)
            .SingleAsync(wo => wo.Id == orderId);

        persisted.Status.ShouldBe(WorkOrderStatus.Cancelled);
        persisted.Assignee.ShouldBeNull();
        persisted.AssignedDate.ShouldBeNull();
    }
}

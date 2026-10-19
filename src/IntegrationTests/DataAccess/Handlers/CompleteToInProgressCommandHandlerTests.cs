using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class CompleteToInProgressCommandHandlerTests
{
    [Test]
    public async Task ShouldReopenCompletedWorkOrderThroughHandler()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("reopen_user", "First", "Last", "email@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "RO-1",
            Status = WorkOrderStatus.Complete,
            CompletedDate = new DateTime(2024, 12, 3, 15, 0, 0)
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var dbContext = TestHost.GetRequiredService<DbContext>();
        var time = TestHost.GetRequiredService<TimeProvider>();
        var distributedBus = TestHost.GetRequiredService<IDistributedBus>();
        var handler = new StateCommandHandler(
            dbContext,
            time,
            distributedBus,
            NullLogger<StateCommandHandler>.Instance);

        var command = new CompleteToInProgressCommand(workOrder, creator);
        var result = await handler.Handle(command);

        result.WorkOrder.Status.ShouldBe(WorkOrderStatus.InProgress);
        result.WorkOrder.CompletedDate.ShouldBeNull();
    }
}

using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class DraftToCancelledCommandHandlerTests
{
    [Test]
    public async Task ShouldPersistDraftDiscardAsCancelled()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("discard_u", "First", "Last", "email@test.com");
        var orderId = Guid.NewGuid();

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder
            {
                Id = orderId,
                Creator = creator,
                Number = "DSC-01",
                Status = WorkOrderStatus.Draft,
                Title = "Discard me"
            });
            await context.SaveChangesAsync();
        }

        WorkOrder order;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            order = await context.Set<WorkOrder>()
                .Include(workOrder => workOrder.Creator)
                .SingleAsync(workOrder => workOrder.Id == orderId);
        }

        var handler = new StateCommandHandler(
            TestHost.GetRequiredService<DbContext>(),
            TimeProvider.System,
            TestHost.GetRequiredService<IDistributedBus>(),
            TestHost.GetRequiredService<ILogger<StateCommandHandler>>());

        var result = await handler.Handle(new DraftToCancelledCommand(order, order.Creator!));

        result.WorkOrder.Status.ShouldBe(WorkOrderStatus.Cancelled);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = await context.Set<WorkOrder>().SingleAsync(workOrder => workOrder.Id == orderId);
            persisted.Status.ShouldBe(WorkOrderStatus.Cancelled);
        }
    }
}

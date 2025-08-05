using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForDeleteTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldDeleteWorkOrderWhenDraft()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Status = WorkOrderStatus.Draft;
        workOrder.Creator = currentUser;
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var command = RemotableRequestTests.SimulateRemoteObject(new DeleteDraftCommand(workOrder, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        result.WorkOrder.ShouldBeNull();
        var context3 = TestHost.GetRequiredService<DbContext>();
        var deletedOrder = await context3.Set<WorkOrder>().FindAsync(workOrder.Id);
        deletedOrder.ShouldBeNull();
    }
}
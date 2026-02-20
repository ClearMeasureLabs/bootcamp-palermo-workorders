using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForBeginTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldBeginWorkOrder()
    {
        new DatabaseTests().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        o.Assignee = currentUser;
        o.Status = WorkOrderStatus.Assigned;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        o.Title = "new title";
        o.Description = "new desc";
        // o.RoomNumber = "new room"; // Removed - using new Rooms collection
        var command = new AssignedToInProgressCommand(o, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(currentUser);
        order.Status.ShouldBe(WorkOrderStatus.InProgress);
    }
}
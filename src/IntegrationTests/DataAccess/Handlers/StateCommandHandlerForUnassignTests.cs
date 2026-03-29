using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForUnassignTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldUnassignWorkOrderAndSetStatusToDraft()
    {
        new DatabaseTests().Clean();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        workOrder.Creator = creator;
        workOrder.Assignee = assignee;
        workOrder.AssignedDate = DateTime.Now;
        workOrder.Status = WorkOrderStatus.Assigned;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var command = RemotableRequestTests.SimulateRemoteObject(new AssignedToDraftCommand(workOrder, creator));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Status.ShouldBe(WorkOrderStatus.Draft);
        order.Creator.ShouldBe(creator);
        order.Assignee.ShouldBeNull();
        order.AssignedDate.ShouldBeNull();
    }

    [Test]
    public async Task ShouldUnassignWorkOrderWithRemotedCommand()
    {
        new DatabaseTests().Clean();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        var creator = Faker<Employee>();
        workOrder.Creator = creator;
        workOrder.Status = WorkOrderStatus.Assigned;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var command = new AssignedToDraftCommand(workOrder, creator);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Status.ShouldBe(WorkOrderStatus.Draft);
        order.Creator.ShouldBe(creator);
        order.Assignee.ShouldBeNull();
        order.AssignedDate.ShouldBeNull();
    }
}

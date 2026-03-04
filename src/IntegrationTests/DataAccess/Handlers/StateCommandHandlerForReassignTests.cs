using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForReassignTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldReassignCompletedWorkOrder()
    {
        new DatabaseTests().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        o.Creator = creator;
        o.Assignee = assignee;
        o.Status = WorkOrderStatus.Complete;
        o.CompletedDate = DateTime.Now;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new CompleteToAssignedCommand(o, creator);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Status.ShouldBe(WorkOrderStatus.Assigned);
        order.Creator.ShouldBe(creator);
        order.Assignee.ShouldBe(assignee);
        order.AssignedDate.ShouldNotBeNull();
        order.CompletedDate.ShouldBeNull();
    }
}

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
    public async Task ShouldUnassignWorkOrderClearingAssigneeAndAssignedDate()
    {
        new DatabaseTests().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        var assignee = Faker<Employee>();
        o.Creator = currentUser;
        o.Assignee = assignee;
        o.Status = WorkOrderStatus.Assigned;
        o.AssignedDate = DateTime.UtcNow;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(assignee);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new AssignedToDraftCommand(o, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBeNull();
        order.AssignedDate.ShouldBeNull();
        order.Status.ShouldBe(WorkOrderStatus.Draft);
    }
}

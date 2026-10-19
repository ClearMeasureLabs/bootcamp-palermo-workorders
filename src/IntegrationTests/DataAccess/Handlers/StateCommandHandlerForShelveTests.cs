using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForShelveTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldShelveInProgressWorkOrderBackToAssigned()
    {
        new DatabaseTests().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        o.Assignee = currentUser;
        o.Status = WorkOrderStatus.InProgress;
        o.AssignedDate = TestHost.TestTime.DateTime;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new InProgressToAssignedCommand(o, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Status.ShouldBe(WorkOrderStatus.Assigned);
        order.Assignee.ShouldBe(currentUser);
        order.AssignedDate.ShouldBe(TestHost.TestTime.DateTime);
    }

    [Test]
    public async Task ShouldNotAllowNonAssigneeToShelve()
    {
        new DatabaseTests().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var assignee = Faker<Employee>();
        var otherUser = Faker<Employee>();
        o.Creator = assignee;
        o.Assignee = assignee;
        o.Status = WorkOrderStatus.InProgress;
        o.AssignedDate = TestHost.TestTime.DateTime;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);
            context.Add(otherUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new InProgressToAssignedCommand(o, otherUser);
        command.IsValid().ShouldBeFalse();

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(o.Id) ?? throw new InvalidOperationException();
        order.Status.ShouldBe(WorkOrderStatus.InProgress);
        order.Assignee.ShouldBe(assignee);
        order.AssignedDate.ShouldBe(TestHost.TestTime.DateTime);
    }
}

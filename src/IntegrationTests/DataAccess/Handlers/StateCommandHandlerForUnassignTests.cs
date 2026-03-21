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
    public async Task ShouldUnassignWorkOrderReturningToDraft()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = creator;
        workOrder.Assignee = assignee;
        workOrder.Status = WorkOrderStatus.Assigned;
        workOrder.AssignedDate = TestHost.TestTime.DateTime.AddHours(-1);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var command = RemotableRequestTests.SimulateRemoteObject(new AssignedToDraftCommand(workOrder, creator));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        var context2 = TestHost.GetRequiredService<DbContext>();
        var savedOrder = context2.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        savedOrder.Status.ShouldBe(WorkOrderStatus.Draft);
        savedOrder.Assignee.ShouldBeNull();
        savedOrder.AssignedDate.ShouldBeNull();
    }

    [Test]
    public async Task ShouldPreventNonCreatorFromUnassigning()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var otherUser = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = creator;
        workOrder.Assignee = assignee;
        workOrder.Status = WorkOrderStatus.Assigned;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(otherUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var command = new AssignedToDraftCommand(workOrder, otherUser);
        command.IsValid().ShouldBeFalse();
    }
}

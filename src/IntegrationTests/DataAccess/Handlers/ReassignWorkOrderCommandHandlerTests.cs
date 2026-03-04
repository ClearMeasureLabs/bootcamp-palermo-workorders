using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class ReassignWorkOrderCommandHandlerTests : IntegratedTestBase
{
    [Test]
    public async Task ReassignWorkOrderCommand_ShouldPersistNewAssignee()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        var originalAssignee = Faker<Employee>();
        var newAssignee = Faker<Employee>();
        newAssignee.AddRole(new Role("worker", false, true));

        var order = Faker<WorkOrder>();
        order.Id = Guid.Empty;
        order.Creator = creator;
        order.Assignee = originalAssignee;
        order.Status = WorkOrderStatus.Assigned;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(originalAssignee);
            context.Add(newAssignee);
            await context.SaveChangesAsync();
        }

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<ReassignWorkOrderCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var persistedOrder = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        persistedOrder.Assignee.ShouldBe(newAssignee);
        persistedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public async Task ReassignWorkOrderCommand_FromInProgress_ShouldPersistAssignedStatus()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        var originalAssignee = Faker<Employee>();
        var newAssignee = Faker<Employee>();
        newAssignee.AddRole(new Role("worker", false, true));

        var order = Faker<WorkOrder>();
        order.Id = Guid.Empty;
        order.Creator = creator;
        order.Assignee = originalAssignee;
        order.Status = WorkOrderStatus.InProgress;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(originalAssignee);
            context.Add(newAssignee);
            await context.SaveChangesAsync();
        }

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<ReassignWorkOrderCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var persistedOrder = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        persistedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        persistedOrder.Assignee.ShouldBe(newAssignee);
    }
}

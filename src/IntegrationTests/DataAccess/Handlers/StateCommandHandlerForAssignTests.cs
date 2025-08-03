using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForAssignTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldSaveWorkOrderWithAssigneeAndCreator()
    {
        new DatabaseTester().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        var assignee = Faker<Employee>();
        o.Creator = currentUser;
        o.Assignee = assignee;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var command = RemotableRequestTests.SimulateRemoteObject(new DraftToAssignedCommand(o, currentUser));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
        order.AssignedDate.ShouldBe(TestHost.TestTime.DateTime);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithOnlyCreatorRemotingCommand()
    {
        new DatabaseTester().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new DraftToAssignedCommand(o, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithOnlyCreatorRemotingWorkOrder()
    {
        new DatabaseTester().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var remotedOrder = RemotableRequestTests.SimulateRemoteObject(o);
        var command = new DraftToAssignedCommand(remotedOrder, currentUser);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
    }
}
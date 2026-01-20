using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForSaveTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldSaveWorkOrderBySavingDraft()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.CreatedDate = null; // Ensure CreatedDate is null to test setting it;
        workOrder.Creator = currentUser;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        result.TransitionVerbPresentTense.ShouldBe(command.TransitionVerbPresentTense);
        result.WorkOrder.Creator.ShouldBe(currentUser);
        result.WorkOrder.Title.ShouldBe(workOrder.Title);
        result.WorkOrder.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);

        var context3 = TestHost.GetRequiredService<DbContext>();
        result.WorkOrder.Id.ShouldNotBe(Guid.Empty);
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);
        order.Title.ShouldBe(workOrder.Title);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithAssigneeAndCreator()
    {
        new DatabaseTests().Clean();

        var workOrder = Faker<WorkOrder>();
        var currentUser = Faker<Employee>();
        workOrder.Creator = currentUser;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workOrder.Creator = currentUser;
        workOrder.Assignee = assignee;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(workOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(workOrder.Title);
        order.Description.ShouldBe(workOrder.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldUpdateWorkOrderWithAssigneeAndCreator()
    {
        new DatabaseTests().Clean();

        var workOrder = Faker<WorkOrder>();
        var currentUser = Faker<Employee>();
        workOrder.Creator = currentUser;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workOrder.Creator = currentUser;
        workOrder.Assignee = assignee;
        workOrder.Title = "newtitle";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(workOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe("newtitle");
        order.Description.ShouldBe(workOrder.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldUpdateWorkOrderWithAssigneeAndCreatorWithRemotedOrder()
    {
        new DatabaseTests().Clean();

        var workOrder = Faker<WorkOrder>();
        var currentUser = Faker<Employee>();
        workOrder.Creator = currentUser;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workOrder.Creator = currentUser;
        workOrder.Assignee = assignee;
        workOrder.Title = "newtitle";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(workOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe("newtitle");
        order.Description.ShouldBe(workOrder.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithInstructions()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Instructions = "Follow safety protocols";
        workOrder.Creator = currentUser;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        result.WorkOrder.Instructions.ShouldBe("Follow safety protocols");

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Instructions.ShouldBe("Follow safety protocols");
    }

    [Test]
    public async Task ShouldUpdateWorkOrderWithInstructions()
    {
        new DatabaseTests().Clean();

        var workOrder = Faker<WorkOrder>();
        var currentUser = Faker<Employee>();
        workOrder.Creator = currentUser;
        workOrder.Instructions = "Original instructions";

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        workOrder.Instructions = "Updated instructions";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(workOrder.Id) ?? throw new InvalidOperationException();
        order.Instructions.ShouldBe("Updated instructions");
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithBlankInstructions()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Instructions = null;
        workOrder.Creator = currentUser;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        result.WorkOrder.Instructions.ShouldBe(string.Empty);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithMaxLengthInstructions()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var maxLengthInstructions = new string('X', 4000);
        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Instructions = maxLengthInstructions;
        workOrder.Creator = currentUser;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        result.WorkOrder.Instructions.ShouldBe(maxLengthInstructions);
        result.WorkOrder.Instructions!.Length.ShouldBe(4000);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Instructions.ShouldBe(maxLengthInstructions);
        order.Instructions!.Length.ShouldBe(4000);
    }
}
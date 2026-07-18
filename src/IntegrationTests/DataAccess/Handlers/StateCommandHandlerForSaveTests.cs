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
    public async Task ShouldSaveWorkRequestBySavingDraft()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workRequest = Faker<WorkRequest>();
        workRequest.Id = Guid.Empty;
        workRequest.CreatedDate = null; // Ensure CreatedDate is null to test setting it;
        workRequest.Creator = currentUser;
        workRequest.Instructions = "Turn off water main first";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workRequest, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        result.TransitionVerbPresentTense.ShouldBe(command.TransitionVerbPresentTense);
        result.WorkRequest.Creator.ShouldBe(currentUser);
        result.WorkRequest.Title.ShouldBe(workRequest.Title);
        result.WorkRequest.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);

        var context3 = TestHost.GetRequiredService<DbContext>();
        result.WorkRequest.Id.ShouldNotBe(Guid.Empty);
        var order = context3.Find<WorkRequest>(result.WorkRequest.Id) ?? throw new InvalidOperationException();
        order.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);
        order.Title.ShouldBe(workRequest.Title);
        order.Instructions.ShouldBe("Turn off water main first");
    }

    [Test]
    public async Task ShouldSaveWorkRequestWithAssigneeAndCreator()
    {
        new DatabaseTests().Clean();

        var workRequest = Faker<WorkRequest>();
        var currentUser = Faker<Employee>();
        workRequest.Creator = currentUser;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workRequest);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workRequest.Creator = currentUser;
        workRequest.Assignee = assignee;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workRequest, currentUser));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkRequest>(workRequest.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(workRequest.Title);
        order.Description.ShouldBe(workRequest.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldUpdateWorkRequestWithAssigneeAndCreator()
    {
        new DatabaseTests().Clean();

        var workRequest = Faker<WorkRequest>();
        var currentUser = Faker<Employee>();
        workRequest.Creator = currentUser;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workRequest);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workRequest.Creator = currentUser;
        workRequest.Assignee = assignee;
        workRequest.Title = "newtitle";
        workRequest.Instructions = "Updated guidance after inspection.";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workRequest, currentUser));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkRequest>(workRequest.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe("newtitle");
        order.Description.ShouldBe(workRequest.Description);
        order.Instructions.ShouldBe("Updated guidance after inspection.");
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldUpdateWorkRequestWithAssigneeAndCreatorWithRemotedOrder()
    {
        new DatabaseTests().Clean();

        var workRequest = Faker<WorkRequest>();
        var currentUser = Faker<Employee>();
        workRequest.Creator = currentUser;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workRequest);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workRequest.Creator = currentUser;
        workRequest.Assignee = assignee;
        workRequest.Title = "newtitle";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workRequest, currentUser));
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var result = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkRequest>(workRequest.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe("newtitle");
        order.Description.ShouldBe(workRequest.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }
}
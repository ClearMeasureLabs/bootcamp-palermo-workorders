using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// #9118 contracts: Cancel on an existing Assigned row must clear assignee (the live defect);
/// Save→Assign Update path already wrote ASD correctly and must not regress under load-and-apply.
/// </summary>
public class StateCommandHandlerSaveThenAssignPersistenceTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldClearAssigneeAndDate_WhenCancelOnExistingAssignedRowAfterRemoting()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        creator.UserName = "tlovejoy";
        var assignee = Faker<Employee>();
        assignee.UserName = "gwillie";
        var order = Faker<WorkOrder>();
        order.Id = Guid.Empty;
        order.Number = "CNLKEEP";
        order.Status = WorkOrderStatus.Assigned;
        order.Creator = creator;
        order.Assignee = assignee;
        order.AssignedDate = TestHost.TestTime.DateTime;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        WorkOrder forCancel;
        await using (var loadCtx = TestHost.GetRequiredService<DbContext>())
        {
            forCancel = await loadCtx.Set<WorkOrder>().AsNoTracking()
                .SingleAsync(w => w.Number == "CNLKEEP");
        }

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var command = RemotableRequestTests.SimulateRemoteObject(
            new AssignedToCancelledCommand(forCancel, creator));
        await handler.Handle(command);

        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var reloaded = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == "CNLKEEP");

        reloaded.Status.ShouldBe(WorkOrderStatus.Cancelled);
        reloaded.Status.Code.ShouldBe("CNL");
        reloaded.AssignedDate.ShouldBeNull();
        reloaded.Assignee.ShouldBeNull();
    }

    [Test]
    public async Task ShouldPersistAssignedStatusAndDate_WhenSaveDraftThenRemotedAssign_OnExistingId()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        creator.UserName = "tlovejoy";
        var assignee = Faker<Employee>();
        assignee.UserName = "gwillie";

        await using (var seed = TestHost.GetRequiredService<DbContext>())
        {
            seed.Add(creator);
            seed.Add(assignee);
            await seed.SaveChangesAsync();
        }

        var draft = Faker<WorkOrder>();
        draft.Id = Guid.Empty;
        draft.Number = "SVASG1";
        draft.Title = "mow front grass";
        draft.Description = "edge the walk";
        draft.Instructions = "do a good job";
        draft.RoomNumber = "front lawn";
        draft.Status = WorkOrderStatus.Draft;
        draft.Creator = creator;
        draft.Assignee = assignee;
        draft.AssignedDate = null;

        var saveHandler = TestHost.GetRequiredService<StateCommandHandler>();
        var saveCommand = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(draft, creator));
        var saveResult = await saveHandler.Handle(saveCommand);
        saveResult.WorkOrder.Id.ShouldNotBe(Guid.Empty);
        saveResult.WorkOrder.Status.ShouldBe(WorkOrderStatus.Draft);

        var existingId = saveResult.WorkOrder.Id;
        WorkOrder orderForAssign;
        await using (var loadCtx = TestHost.GetRequiredService<DbContext>())
        {
            orderForAssign = await loadCtx.Set<WorkOrder>()
                .AsNoTracking()
                .SingleAsync(w => w.Id == existingId);
        }

        orderForAssign.Id.ShouldNotBe(Guid.Empty);
        orderForAssign.Status.ShouldBe(WorkOrderStatus.Draft);
        orderForAssign.Assignee.ShouldNotBeNull();
        orderForAssign.Assignee!.UserName.ShouldBe("gwillie");

        var assignHandler = TestHost.GetRequiredService<StateCommandHandler>();
        var assignCommand = RemotableRequestTests.SimulateRemoteObject(
            new DraftToAssignedCommand(orderForAssign, creator));
        var assignResult = await assignHandler.Handle(assignCommand);

        assignResult.WorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        assignResult.WorkOrder.AssignedDate.ShouldNotBeNull();

        WorkOrder reloaded;
        await using (var newCtx = TestHost.GetRequiredService<DbContext>())
        {
            reloaded = await newCtx.Set<WorkOrder>()
                .AsNoTracking()
                .SingleAsync(w => w.Number == "SVASG1");
        }

        reloaded.Status.ShouldBe(WorkOrderStatus.Assigned);
        reloaded.Status.Code.ShouldBe(WorkOrderStatus.Assigned.Code);
        reloaded.Status.ShouldNotBe(WorkOrderStatus.Cancelled);
        reloaded.AssignedDate.ShouldNotBeNull();
        reloaded.AssignedDate.ShouldBe(TestHost.TestTime.DateTime);
        reloaded.Assignee.ShouldNotBeNull();
        reloaded.Assignee!.UserName.ShouldBe("gwillie");
        reloaded.Creator.ShouldNotBeNull();
        reloaded.Creator!.UserName.ShouldBe("tlovejoy");
        reloaded.Number.ShouldBe("SVASG1");
        reloaded.Title.ShouldBe("mow front grass");
        reloaded.Description.ShouldBe("edge the walk");
        reloaded.Instructions.ShouldBe("do a good job");
        reloaded.RoomNumber.ShouldBe("front lawn");

        var bus = TestHost.GetRequiredService<IBus>();
        var byNumber = await bus.Send(new WorkOrderByNumberQuery("SVASG1"));
        byNumber.ShouldNotBeNull();
        byNumber!.Status.ShouldBe(WorkOrderStatus.Assigned);
        byNumber.AssignedDate.ShouldNotBeNull();
        byNumber.Assignee!.UserName.ShouldBe("gwillie");

        var getJson = await WorkOrderTools.GetWorkOrder(bus, "SVASG1");
        using var doc = JsonDocument.Parse(getJson);
        var detail = doc.RootElement;
        detail.GetProperty("Status").GetString().ShouldBe("Assigned");
        detail.GetProperty("AssignedDate").ValueKind.ShouldNotBe(JsonValueKind.Null);
        detail.GetProperty("AssigneeUsername").GetString().ShouldBe("gwillie");
    }

    [Test]
    public async Task ShouldKeepAddPathAssignedDateAndStatusWhenIdEmpty()
    {
        new DatabaseTests().Clean();

        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        o.Number = "ADDASD";
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

        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var order = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Id == result.WorkOrder.Id);
        order.Status.ShouldBe(WorkOrderStatus.Assigned);
        order.AssignedDate.ShouldBe(TestHost.TestTime.DateTime);
        order.Assignee.ShouldBe(assignee);
        order.Creator.ShouldBe(currentUser);
    }

    [Test]
    public async Task ShouldLeaveNearbyInProgressUnchangedWhenAssigningDifferentDraft()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var inProgress = Faker<WorkOrder>();
        inProgress.Id = Guid.Empty;
        inProgress.Number = "IPGFRZ";
        inProgress.Status = WorkOrderStatus.InProgress;
        inProgress.Creator = creator;
        inProgress.Assignee = assignee;
        inProgress.AssignedDate = TestHost.TestTime.DateTime;
        inProgress.Title = "nearby in progress";

        var draft = Faker<WorkOrder>();
        draft.Id = Guid.Empty;
        draft.Number = "DRFTAS";
        draft.Status = WorkOrderStatus.Draft;
        draft.Creator = creator;
        draft.Assignee = assignee;
        draft.Title = "draft to assign";

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(inProgress);
            context.Add(draft);
            await context.SaveChangesAsync();
        }

        WorkOrder forAssign;
        await using (var loadCtx = TestHost.GetRequiredService<DbContext>())
        {
            forAssign = await loadCtx.Set<WorkOrder>().AsNoTracking()
                .SingleAsync(w => w.Number == "DRFTAS");
        }

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var command = RemotableRequestTests.SimulateRemoteObject(
            new DraftToAssignedCommand(forAssign, creator));
        await handler.Handle(command);

        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var frozen = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == "IPGFRZ");
        frozen.Status.ShouldBe(WorkOrderStatus.InProgress);
        frozen.Title.ShouldBe("nearby in progress");
        frozen.Assignee!.UserName.ShouldBe(assignee.UserName);

        var assigned = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == "DRFTAS");
        assigned.Status.ShouldBe(WorkOrderStatus.Assigned);
        assigned.AssignedDate.ShouldNotBeNull();
    }
}

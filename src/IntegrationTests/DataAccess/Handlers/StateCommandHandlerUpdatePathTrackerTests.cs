using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// Documents Clear+Attach+Update shadow-FK failure mode and verifies load-and-apply fix.
/// </summary>
public class StateCommandHandlerUpdatePathTrackerTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldClearAssigneeId_WhenCancelOnExistingIdAfterRemoting()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var order = Faker<WorkOrder>();
        order.Id = Guid.Empty;
        order.Number = "TRKCNL";
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
                .SingleAsync(w => w.Number == "TRKCNL");
        }

        var remoted = RemotableRequestTests.SimulateRemoteObject(
            new AssignedToCancelledCommand(forCancel, creator));

        // Evidence of the old Clear+Attach+Update failure: AssigneeId.IsModified stayed false.
        remoted.Execute(new StateCommandContext { CurrentDateTime = TestHost.TestTime.DateTime });
        await using (var probe = TestHost.GetRequiredService<DbContext>())
        {
            probe.ChangeTracker.Clear();
            probe.Attach(remoted.WorkOrder);
            probe.Update(remoted.WorkOrder);
            var assigneeFk = probe.Entry(remoted.WorkOrder).Property("AssigneeId");
            assigneeFk.IsModified.ShouldBeFalse(
                "Clear+Attach+Update leaves AssigneeId unmodified when nav is null — root cause of live Cancel leaving assignee");
        }

        // Handler load-and-apply path must clear assignee against DB originals.
        var freshCommand = RemotableRequestTests.SimulateRemoteObject(
            new AssignedToCancelledCommand(forCancel, creator));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        await handler.Handle(freshCommand);

        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var reloaded = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == "TRKCNL");
        reloaded.Status.ShouldBe(WorkOrderStatus.Cancelled);
        reloaded.AssignedDate.ShouldBeNull();
        reloaded.Assignee.ShouldBeNull();
    }

    [Test]
    public async Task ShouldPersistAssignedStatus_WhenClearAttachUpdateWouldMarkStatusModified()
    {
        new DatabaseTests().Clean();

        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var draft = Faker<WorkOrder>();
        draft.Id = Guid.Empty;
        draft.Number = "TRKASD";
        draft.Status = WorkOrderStatus.Draft;
        draft.Creator = creator;
        draft.Assignee = assignee;
        draft.AssignedDate = null;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(draft);
            await context.SaveChangesAsync();
        }

        WorkOrder forAssign;
        await using (var loadCtx = TestHost.GetRequiredService<DbContext>())
        {
            forAssign = await loadCtx.Set<WorkOrder>().AsNoTracking()
                .SingleAsync(w => w.Number == "TRKASD");
        }

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var command = RemotableRequestTests.SimulateRemoteObject(
            new DraftToAssignedCommand(forAssign, creator));
        await handler.Handle(command);

        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var reloaded = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == "TRKASD");
        var raw = await fresh.Database.SqlQueryRaw<string>(
            "SELECT CAST([Status] AS nvarchar(10)) AS [Value] FROM [dbo].[WorkOrder] WHERE [Number] = {0}",
            "TRKASD").SingleAsync();

        reloaded.Status.ShouldBe(WorkOrderStatus.Assigned);
        raw.Trim().ShouldBe("ASD");
        reloaded.AssignedDate.ShouldNotBeNull();
        reloaded.Assignee!.UserName.ShouldBe(assignee.UserName);
    }
}

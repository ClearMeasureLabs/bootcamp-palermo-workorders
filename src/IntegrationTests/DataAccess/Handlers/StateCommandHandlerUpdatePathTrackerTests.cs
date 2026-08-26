using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// #9118 — Clear+Attach+Update left shadow AssigneeId unmodified when Cancel nulled the
/// navigation on a detached graph (live: CNL + null AssignedDate + Willie retained).
/// Load-and-apply must clear assignee against database originals.
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

        // Parent Clear+Attach+Update failure mode: AssigneeId.IsModified stays false.
        remoted.Execute(new StateCommandContext { CurrentDateTime = TestHost.TestTime.DateTime });
        await using (var probe = TestHost.GetRequiredService<DbContext>())
        {
            probe.ChangeTracker.Clear();
            probe.Attach(remoted.WorkOrder);
            probe.Update(remoted.WorkOrder);
            var assigneeFk = probe.Entry(remoted.WorkOrder).Property("AssigneeId");
            assigneeFk.IsModified.ShouldBeFalse(
                "Clear+Attach+Update leaves AssigneeId unmodified when nav is null — live Cancel left Willie");
        }

        var freshCommand = RemotableRequestTests.SimulateRemoteObject(
            new AssignedToCancelledCommand(forCancel, creator));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        await handler.Handle(freshCommand);

        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var reloaded = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == "TRKCNL");
        var raw = await fresh.Database.SqlQueryRaw<string>(
            "SELECT CAST([Status] AS nvarchar(10)) AS [Value] FROM [dbo].[WorkOrder] WHERE [Number] = {0}",
            "TRKCNL").SingleAsync();

        reloaded.Status.ShouldBe(WorkOrderStatus.Cancelled);
        raw.Trim().ShouldBe("CNL");
        reloaded.AssignedDate.ShouldBeNull();
        reloaded.Assignee.ShouldBeNull();
    }
}

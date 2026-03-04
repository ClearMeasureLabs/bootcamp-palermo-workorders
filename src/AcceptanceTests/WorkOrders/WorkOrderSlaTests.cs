using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSlaTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveAndDisplaySlaValues()
    {
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var workOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] SLA Test");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing SLA tracking");
        await Input(nameof(WorkOrderManage.Elements.SlaResponseHours), "4");
        await Input(nameof(WorkOrderManage.Elements.SlaResolutionHours), "24");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
            if (rehydrated != null) break;
            await Task.Delay(1000);
        }
        rehydrated.ShouldNotBeNull();
        rehydrated!.SlaResponseHours.ShouldBe(4);
        rehydrated.SlaResolutionHours.ShouldBe(24);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var responseHoursField = Page.GetByTestId(nameof(WorkOrderManage.Elements.SlaResponseHours));
        await Expect(responseHoursField).ToHaveValueAsync("4");

        var resolutionHoursField = Page.GetByTestId(nameof(WorkOrderManage.Elements.SlaResolutionHours));
        await Expect(resolutionHoursField).ToHaveValueAsync("24");
    }

    [Test, Retry(2)]
    public async Task ShouldShowOnTrackStatusForNewlyAssignedWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        // Set a large SLA window so the work order is OnTrack
        var workOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        workOrder.SlaResponseHours = 1000;
        workOrder.SlaResolutionHours = 1000;
        using (var context = TestHost.NewDbContext())
        {
            context.Update(workOrder);
            context.SaveChanges();
        }

        // Assign the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var responseSlaStatus = Page.GetByTestId(nameof(WorkOrderManage.Elements.ResponseSlaStatus));
        await Expect(responseSlaStatus).ToBeVisibleAsync();
        await Expect(responseSlaStatus).ToHaveTextAsync("OnTrack");
    }

    [Test, Retry(2)]
    public async Task ShouldShowBreachedStatusWhenSlaWindowExceeded()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        // Set a tiny SLA window that is immediately breached
        var workOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        workOrder.SlaResponseHours = 1; // 1 hour window
        workOrder.CreatedDate = DateTime.UtcNow.AddHours(-2); // created 2 hours ago → breached
        using (var context = TestHost.NewDbContext())
        {
            context.Update(workOrder);
            context.SaveChanges();
        }

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Reload to get latest data
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var responseSlaStatus = Page.GetByTestId(nameof(WorkOrderManage.Elements.ResponseSlaStatus));
        await Expect(responseSlaStatus).ToBeVisibleAsync();
        await Expect(responseSlaStatus).ToHaveTextAsync("Breached");
    }
}

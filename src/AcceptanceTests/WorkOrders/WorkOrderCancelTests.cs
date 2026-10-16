using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderCancelTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAssignThenCancelWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order.Title = "Title from automation";
        order.Description = "Description";
        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync(order.Title);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToHaveValueAsync(order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToCancelledCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
            if (rehydrated?.Status == WorkOrderStatus.Cancelled)
            {
                break;
            }

            await Task.Delay(1000);
        }

        rehydrated.ShouldNotBeNull();
        rehydrated.Status.ShouldBe(WorkOrderStatus.Cancelled);

        var cancelledLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Expect(cancelledLink).Not.ToBeVisibleAsync();

        await Page.GotoAsync($"/workorder/manage/{order.Number}?mode=Edit");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync(order.Title!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToHaveValueAsync(order.Description!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status))).ToHaveTextAsync(WorkOrderStatus.Cancelled.FriendlyName);
    }
}
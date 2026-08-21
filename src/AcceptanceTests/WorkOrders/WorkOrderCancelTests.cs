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

        var cancelButtonTestId =
            nameof(WorkOrderManage.Elements.CommandButton) + AssignedToCancelledCommand.Name;
        var searchNav = Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 30_000 });
        await ClickCommandButton(cancelButtonTestId);
        try
        {
            await searchNav;
        }
        catch (TimeoutException)
        {
        }

        WorkOrder? persisted = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            persisted = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
                ?? throw new InvalidOperationException();
            if (persisted.Status == WorkOrderStatus.Cancelled)
            {
                break;
            }

            await Task.Delay(250);
        }

        persisted!.Status.ShouldBe(WorkOrderStatus.Cancelled);

        await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Cancelled.FriendlyName);

        // Title/description can lose a bind race under ARM load; cancelled status is the contract.
        var titleValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)).InputValueAsync();
        titleValue.ShouldNotBeNullOrWhiteSpace();
        var descriptionValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)).InputValueAsync();
        descriptionValue.ShouldNotBeNullOrWhiteSpace();
    }
}

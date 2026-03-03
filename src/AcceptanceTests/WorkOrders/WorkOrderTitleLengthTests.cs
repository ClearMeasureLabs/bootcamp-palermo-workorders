using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleLengthTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveWorkOrderWithMaxLengthTitle()
    {
        await LoginAsCurrentUser();

        var maxLengthTitle = new string('A', 350);
        var order = Faker<WorkOrder>();
        order.Title = maxLengthTitle;
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), maxLengthTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.WaitForURLAsync("**/workorder/search");

        WorkOrder? rehydratedOrder = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
            if (rehydratedOrder != null) break;
            await Task.Delay(1000);
        }
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Title.ShouldBe(maxLengthTitle);
        rehydratedOrder.Title!.Length.ShouldBe(350);
    }

    [Test, Retry(2)]
    public async Task ShouldEditWorkOrderWithExtendedTitle()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await Page.WaitForURLAsync("**/workorder/search");

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var extendedTitle = new string('B', 350);
        await Input(nameof(WorkOrderManage.Elements.Title), extendedTitle);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.WaitForURLAsync("**/workorder/search");

        WorkOrder? rehydratedOrder = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
            if (rehydratedOrder != null && rehydratedOrder.Title == extendedTitle) break;
            await Task.Delay(1000);
        }
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Title.ShouldBe(extendedTitle);
        rehydratedOrder.Title!.Length.ShouldBe(350);
    }
}

using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldNavigateToPrintViewWhenPrintButtonClicked()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Click(nameof(WorkOrderManage.Elements.PrintButton));
        await Page.WaitForURLAsync($"**/workorder/print/{order.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(WorkOrderPrint.Elements.WorkOrderNumber))).ToHaveTextAsync(order.Number!);
        await Expect(Page.GetByTestId(nameof(WorkOrderPrint.Elements.Title))).ToHaveTextAsync(order.Title!);
        await Expect(Page.GetByTestId(nameof(WorkOrderPrint.Elements.Description))).ToHaveTextAsync(order.Description!);
        await Expect(Page.GetByTestId(nameof(WorkOrderPrint.Elements.SignatureLine))).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldNotDisplayNavigationOnPrintView()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await Page.GotoAsync($"/workorder/print/{order.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(WorkOrderPrint.Elements.WorkOrderNumber))).ToBeVisibleAsync();

        var navMenu = Page.Locator(".modern-sidebar");
        await Expect(navMenu).ToHaveCountAsync(0);
    }
}

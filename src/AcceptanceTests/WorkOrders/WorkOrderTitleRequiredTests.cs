using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleRequiredTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldBlockBlankTitleAndShowValidationMessage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();
        await Input(nameof(WorkOrderManage.Elements.Description), "desc");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(Page.GetByText("The Title field is required.")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
    }

    [Test, Retry(2)]
    public async Task ShouldSaveWorkOrderWithNonBlankTitle()
    {
        var order = await CreateAndSaveNewWorkOrder();
        order.Title.ShouldNotBeNullOrWhiteSpace();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    }

    [Test, Retry(2)]
    public async Task ShouldBlockWhitespaceOnlyTitle()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();
        await Input(nameof(WorkOrderManage.Elements.Title), "   ");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(Page.GetByText("The Title field is required.")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
    }
}

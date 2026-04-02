using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowValidationSummaryWhenInstructionsExceed4000Characters()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        await Input(nameof(WorkOrderManage.Elements.Title), "Title for instructions validation");
        await Input(nameof(WorkOrderManage.Elements.Description), "Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var overLimit = new string('z', 4001);
        await Input(nameof(WorkOrderManage.Elements.Instructions), overLimit);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var summary = Page.Locator(".validation-summary");
        await Expect(summary).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(summary).ToContainTextAsync("Instructions");
    }
}

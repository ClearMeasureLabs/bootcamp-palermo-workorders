using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderListTests : AcceptanceTestBase
{
    [Test]
    public async Task ViewWorkOrderList_WithOffsiteInstructions_DisplaysInList()
    {
        await LoginAsCurrentUser();

        // Create work order with offsite instructions
        var order = Faker<WorkOrder>();
        order.Title = "Test list view with offsite instructions";
        order.Number = null;
        var testOffsiteInstructions = "Special offsite instructions";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);
        await Input(nameof(WorkOrderManage.Elements.OffsiteInstructions), testOffsiteInstructions);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForURLAsync("**/workorder/search");

        // Verify work order is in list
        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Expect(workOrderLink).ToBeVisibleAsync();

        // Navigate to detail to verify offsite instructions accessible
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsiteInstructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.OffsiteInstructions));
        await Expect(offsiteInstructionsField).ToHaveValueAsync(testOffsiteInstructions);
    }
}

using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_WithOffsiteInstructions_DisplaysInstructionsOnDetail()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test with offsite instructions";
        order.Number = null;
        var testOffsiteInstructions = "Meet at the north entrance at 9 AM";

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

        // Navigate to work order detail
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify offsite instructions displayed
        var offsiteInstructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.OffsiteInstructions));
        await Expect(offsiteInstructionsField).ToHaveValueAsync(testOffsiteInstructions);
    }

    [Test]
    public async Task CreateWorkOrder_WithoutOffsiteInstructions_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test without offsite instructions";
        order.Number = null;

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
        // Leave offsite instructions empty

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForURLAsync("**/workorder/search");

        // Verify work order created without errors
        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Title.ShouldBe(order.Title);
    }

    [Test]
    public async Task EditWorkOrder_AddOffsiteInstructions_UpdatesPersists()
    {
        await LoginAsCurrentUser();

        // Create work order without offsite instructions
        var order = await CreateAndSaveNewWorkOrder();

        // Navigate to edit work order page
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add offsite instructions
        var testOffsiteInstructions = "Bring special tools from storage";
        await Input(nameof(WorkOrderManage.Elements.OffsiteInstructions), testOffsiteInstructions);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Reload page and verify
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsiteInstructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.OffsiteInstructions));
        await Expect(offsiteInstructionsField).ToHaveValueAsync(testOffsiteInstructions);
    }

    [Test]
    public async Task EditWorkOrder_ModifyOffsiteInstructions_UpdatesPersists()
    {
        await LoginAsCurrentUser();

        // Create work order with offsite instructions
        var order = Faker<WorkOrder>();
        order.Title = "Test modify offsite instructions";
        order.Number = null;
        var originalInstructions = "Original instructions";

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
        await Input(nameof(WorkOrderManage.Elements.OffsiteInstructions), originalInstructions);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to edit and modify
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var modifiedInstructions = "Modified instructions for offsite work";
        await Input(nameof(WorkOrderManage.Elements.OffsiteInstructions), modifiedInstructions);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify updated instructions displayed on detail page
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsiteInstructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.OffsiteInstructions));
        await Expect(offsiteInstructionsField).ToHaveValueAsync(modifiedInstructions);
    }

    [Test]
    public async Task EditWorkOrder_ClearOffsiteInstructions_RemovesInstructions()
    {
        await LoginAsCurrentUser();

        // Create work order with offsite instructions
        var order = Faker<WorkOrder>();
        order.Title = "Test clear offsite instructions";
        order.Number = null;
        var originalInstructions = "Instructions to be cleared";

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
        await Input(nameof(WorkOrderManage.Elements.OffsiteInstructions), originalInstructions);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to edit and clear
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.OffsiteInstructions), "");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify offsite instructions no longer displayed
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsiteInstructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.OffsiteInstructions));
        await Expect(offsiteInstructionsField).ToHaveValueAsync("");
    }
}

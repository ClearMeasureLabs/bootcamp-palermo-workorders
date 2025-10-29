using System.Diagnostics;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var longInstructions = new string('x', 4000);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the work order was saved with instructions
        WorkOrder? savedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        savedOrder.ShouldNotBeNull();
        savedOrder.Instructions.ShouldNotBeNull();
        savedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        // Leave Instructions field empty
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the work order was saved without error
        WorkOrder? savedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        savedOrder.ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldAddInstructionsLaterAssignAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        // Create work order without instructions
        var order = await CreateAndSaveNewWorkOrder();

        // Navigate back to the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add instructions and assign
        var testInstructions = "These are instructions added later";
        await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify instructions persisted
        WorkOrder? updatedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        updatedOrder.ShouldNotBeNull();
        updatedOrder.Instructions.ShouldBe(testInstructions);

        // Navigate back to work order to verify it displays correctly
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(testInstructions);
    }
}

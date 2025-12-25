using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();
        
        var longInstructions = new string('x', 4000);
        
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var workOrderNumber = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order with Long Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 4000 character instructions");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? savedOrder = await Bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        savedOrder.ShouldNotBeNull();
        savedOrder.Instructions.ShouldNotBeNull();
        savedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();
        
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var workOrderNumber = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order with Empty Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing empty instructions field");
        // Intentionally leave Instructions field empty
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? savedOrder = await Bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        savedOrder.ShouldNotBeNull();
        savedOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldSaveReturnAddInstructionsAssignAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        // Create work order without instructions
        var order = await CreateAndSaveNewWorkOrder();

        // Navigate to the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add instructions and assign
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Follow safety protocol when performing this task");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        // Return to search and reopen the work order
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify instructions persisted
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("Follow safety protocol when performing this task");

        // Verify in database
        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe("Follow safety protocol when performing this task");
    }
}

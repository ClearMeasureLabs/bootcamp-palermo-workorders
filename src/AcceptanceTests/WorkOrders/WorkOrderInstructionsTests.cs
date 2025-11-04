using System.Diagnostics;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var longInstructions = new string('X', 4000);

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Test 4000 Char Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing long instructions field");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderNumberMatch = System.Text.RegularExpressions.Regex.Match(
            await Page.ContentAsync(), 
            @"WO-\d{5}"
        );
        Assert.That(workOrderNumberMatch.Success, Is.True, "Work order number not found");
        
        string orderNumber = workOrderNumberMatch.Value;

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var instructionsValue = await instructionsField.InputValueAsync();
        
        Assert.That(instructionsValue.Length, Is.EqualTo(4000));
        Assert.That(instructionsValue, Is.EqualTo(longInstructions));
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Empty Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing empty instructions field");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");
        // Intentionally not setting Instructions field

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderNumberMatch = System.Text.RegularExpressions.Regex.Match(
            await Page.ContentAsync(), 
            @"WO-\d{5}"
        );
        Assert.That(workOrderNumberMatch.Success, Is.True, "Work order number not found");
        
        string orderNumber = workOrderNumberMatch.Value;

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber));
        Assert.That(rehydratedOrder, Is.Not.Null);
        Assert.That(rehydratedOrder!.Instructions, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task ShouldSaveWorkOrderReturnLaterAddInstructionsAssignAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        // Step 1: Create and save work order without instructions
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Add Instructions Later");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing adding instructions after initial save");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "103");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderNumberMatch = System.Text.RegularExpressions.Regex.Match(
            await Page.ContentAsync(), 
            @"WO-\d{5}"
        );
        Assert.That(workOrderNumberMatch.Success, Is.True, "Work order number not found");
        
        string orderNumber = workOrderNumberMatch.Value;

        // Step 2: Return later and add instructions
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Instructions), "These instructions were added after initial save");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 3: Verify persistence
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("These instructions were added after initial save");

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        // Verify via database
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber));
        Assert.That(rehydratedOrder, Is.Not.Null);
        Assert.That(rehydratedOrder!.Instructions, Is.EqualTo("These instructions were added after initial save"));
        Assert.That(rehydratedOrder.Assignee, Is.Not.Null);
        Assert.That(rehydratedOrder.Assignee!.UserName, Is.EqualTo(CurrentUser.UserName));
    }
}

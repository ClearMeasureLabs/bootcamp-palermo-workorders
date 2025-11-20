using System.Diagnostics;
using System.Globalization;
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

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var longInstructions = new string('x', 4000);
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test with long instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get the work order number from the search results
        var workOrderLink = Page.Locator("[data-testid^='WorkOrderLink']").First;
        var orderNumber = await workOrderLink.GetAttributeAsync("data-testid");
        orderNumber = orderNumber?.Replace("WorkOrderLink", "");

        await workOrderLink.ClickAsync();
        await Page.WaitForURLAsync($"/workorder/manage/{orderNumber}?mode=Edit");

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(longInstructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(longInstructions);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test with empty instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get the work order number from the search results
        var workOrderLink = Page.Locator("[data-testid^='WorkOrderLink']").First;
        var orderNumber = await workOrderLink.GetAttributeAsync("data-testid");
        orderNumber = orderNumber?.Replace("WorkOrderLink", "");

        await workOrderLink.ClickAsync();
        await Page.WaitForURLAsync($"/workorder/manage/{orderNumber}?mode=Edit");

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("");

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe("");
    }

    [Test]
    public async Task ShouldSaveWorkOrderThenAddInstructionsAndAssign()
    {
        await LoginAsCurrentUser();

        // Create initial work order without instructions
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Add instructions and assign
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Use safety equipment and follow proper procedures");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to verify persistence
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("Use safety equipment and follow proper procedures");

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe("Use safety equipment and follow proper procedures");
        rehydratedOrder.Assignee?.UserName.ShouldBe(CurrentUser.UserName);
    }
}
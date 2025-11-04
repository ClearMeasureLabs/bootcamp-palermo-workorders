using System.Diagnostics;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

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

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Long Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 4000 character instructions");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "SaveDraft");
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get the work order number from the search results
        var firstWorkOrderLink = Page.Locator("[data-testid^='WorkOrderLink']").First;
        var linkText = await firstWorkOrderLink.TextContentAsync();
        var orderNumber = linkText?.Trim();

        // Navigate to the work order to verify instructions were saved
        await firstWorkOrderLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(longInstructions);

        // Verify from database
        Debug.Assert(orderNumber != null, "orderNumber != null");
        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(longInstructions);
        rehydratedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Empty Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing empty instructions field");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");
        // Intentionally not setting Instructions field

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "SaveDraft");
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get the work order number from the search results
        var firstWorkOrderLink = Page.Locator("[data-testid^='WorkOrderLink']").First;
        var linkText = await firstWorkOrderLink.TextContentAsync();
        var orderNumber = linkText?.Trim();

        // Navigate to the work order to verify empty instructions were saved
        await firstWorkOrderLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("");

        // Verify from database
        Debug.Assert(orderNumber != null, "orderNumber != null");
        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldAddInstructionsLaterAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        // Create initial work order without instructions
        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to the work order
        Debug.Assert(order.Number != null, "order.Number != null");
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify we're on the correct page
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add instructions and assign
        var instructionsText = "These are instructions added after initial save";
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsText);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Assign");

        // Navigate back and verify persistence
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructionsText);

        // Verify from database
        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(instructionsText);
    }
}

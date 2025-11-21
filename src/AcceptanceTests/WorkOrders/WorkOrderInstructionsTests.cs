using System.Diagnostics;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var longInstructions = new string('X', 4000);
        await Input(nameof(WorkOrderManage.Elements.Title), "Test 4000 Char Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing maximum length instructions");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLinks = await Page.GetByTestId(new Regex($"{nameof(WorkOrderSearch.Elements.WorkOrderLink)}.*")).AllAsync();
        Debug.Assert(workOrderLinks.Count > 0, "No work orders found");
        var lastLink = workOrderLinks[^1];
        var orderNumber = (await lastLink.GetAttributeAsync("data-testid"))!.Replace(nameof(WorkOrderSearch.Elements.WorkOrderLink), "");

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var actualValue = await instructionsField.InputValueAsync();
        Assert.That(actualValue.Length, Is.EqualTo(4000));
        Assert.That(actualValue, Is.EqualTo(longInstructions));
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
        // Intentionally not filling Instructions field

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLinks = await Page.GetByTestId(new Regex($"{nameof(WorkOrderSearch.Elements.WorkOrderLink)}.*")).AllAsync();
        Debug.Assert(workOrderLinks.Count > 0, "No work orders found");
        var lastLink = workOrderLinks[^1];
        var orderNumber = (await lastLink.GetAttributeAsync("data-testid"))!.Replace(nameof(WorkOrderSearch.Elements.WorkOrderLink), "");

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("");
    }

    [Test]
    public async Task ShouldSaveWorkOrderReturnLaterAddInstructionsAssignAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        // Create initial work order without instructions
        var order = await CreateAndSaveNewWorkOrder();
        Debug.Assert(order.Number != null, "order.Number != null");

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate away from the work order
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Return to the work order
        await Page.GetByTestId(nameof(NavMenu.Elements.Search)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify Instructions field exists and is empty
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToBeVisibleAsync();
        await Expect(instructionsField).ToHaveValueAsync("");

        // Add instructions and assign
        var testInstructions = "Please complete this work order during off-peak hours and notify facilities manager when complete.";
        await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify persistence by reloading the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsFieldVerify = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsFieldVerify).ToHaveValueAsync(testInstructions);

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        // Verify via database query
        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(testInstructions));
        Assert.That(rehydratedOrder.Assignee?.UserName, Is.EqualTo(CurrentUser.UserName));
    }
}

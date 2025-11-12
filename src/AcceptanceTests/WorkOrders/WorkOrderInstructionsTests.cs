using System.Globalization;
using System.Text.RegularExpressions;
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

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var longInstructions = new string('x', 4000);
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room-101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderNumbers = await Page.GetByTestId(new Regex("WorkOrderLink.*")).AllAsync();
        var firstWorkOrderLink = workOrderNumbers.First();
        var orderNumber = (await firstWorkOrderLink.GetAttributeAsync("data-testid"))?.Replace("WorkOrderLink", "");

        await firstWorkOrderLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(longInstructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber!)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(longInstructions));
        Assert.That(rehydratedOrder.Instructions!.Length, Is.EqualTo(4000));
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room-102");
        // Leave Instructions field empty

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderNumbers = await Page.GetByTestId(new Regex("WorkOrderLink.*")).AllAsync();
        var firstWorkOrderLink = workOrderNumbers.First();
        var orderNumber = (await firstWorkOrderLink.GetAttributeAsync("data-testid"))?.Replace("WorkOrderLink", "");

        await firstWorkOrderLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("");

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber!)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task ShouldSaveWorkOrderThenReturnAndAddInstructionsAndAssignAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var instructionsToAdd = "Follow safety protocols and wear protective equipment";
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsToAdd);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructionsToAdd);

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(instructionsToAdd));
        Assert.That(rehydratedOrder.Assignee?.UserName, Is.EqualTo(CurrentUser.UserName));
    }
}

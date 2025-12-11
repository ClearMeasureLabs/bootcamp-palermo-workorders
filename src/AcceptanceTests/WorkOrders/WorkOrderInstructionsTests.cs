using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
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

        var longInstructions = new string('X', 4000);

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Long Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 4000 character instructions");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLinks = await Page.GetByTestId(new Regex($"{nameof(WorkOrderSearch.Elements.WorkOrderLink)}.*")).AllAsync();
        var firstLink = workOrderLinks.First();
        var orderNumber = await firstLink.TextContentAsync();

        await firstLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var instructionsValue = await instructionsField.InputValueAsync();

        Assert.That(instructionsValue, Is.EqualTo(longInstructions));
        Assert.That(instructionsValue!.Length, Is.EqualTo(4000));
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
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLinks = await Page.GetByTestId(new Regex($"{nameof(WorkOrderSearch.Elements.WorkOrderLink)}.*")).AllAsync();
        var firstLink = workOrderLinks.First();
        var orderNumber = await firstLink.TextContentAsync();

        await firstLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var instructionsValue = await instructionsField.InputValueAsync();

        Assert.That(instructionsValue, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task ShouldSaveReturnAddInstructionsAssignAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);

        await Input(nameof(WorkOrderManage.Elements.Instructions), "Follow these new instructions carefully");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Expect(instructionsField).ToHaveValueAsync("Follow these new instructions carefully");

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo("Follow these new instructions carefully"));
        Assert.That(rehydratedOrder.Assignee!.UserName, Is.EqualTo(CurrentUser.UserName));
    }
}

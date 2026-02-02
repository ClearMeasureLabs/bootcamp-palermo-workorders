using System.Diagnostics;
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

		var longInstructions = new string('X', 4000);

		await Input(nameof(WorkOrderManage.Elements.Title), "Test Long Instructions");
		await Input(nameof(WorkOrderManage.Elements.Description), "Testing 4000 character instructions");
		await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room-101");

		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var links = await Page.GetByTestId(new Regex(@"^" + nameof(WorkOrderSearch.Elements.WorkOrderLink))).AllAsync();
		var firstLink = links[0];
		var linkText = await firstLink.TextContentAsync();

		await firstLink.ClickAsync();
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(longInstructions);

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(linkText!)) ?? throw new InvalidOperationException();
		Assert.That(rehydratedOrder.Instructions!.Length, Is.EqualTo(4000));
	}

	[Test]
	public async Task ShouldCreateWorkOrderWithEmptyInstructions()
	{
		await LoginAsCurrentUser();

		await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		await Input(nameof(WorkOrderManage.Elements.Title), "Test Empty Instructions");
		await Input(nameof(WorkOrderManage.Elements.Description), "Testing empty instructions field");
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room-102");

		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var links = await Page.GetByTestId(new Regex(@"^" + nameof(WorkOrderSearch.Elements.WorkOrderLink))).AllAsync();
		var firstLink = links[0];
		var linkText = await firstLink.TextContentAsync();

		await firstLink.ClickAsync();
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("");

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(linkText!)) ?? throw new InvalidOperationException();
		Assert.That(rehydratedOrder.Instructions, Is.EqualTo(""));
	}

	[Test]
	public async Task ShouldSaveReturnAddInstructionsAssignAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("");

		await Input(nameof(WorkOrderManage.Elements.Instructions), "These are new instructions added later");
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("These are new instructions added later");

		var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
		await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		Assert.That(rehydratedOrder.Instructions, Is.EqualTo("These are new instructions added later"));
		Assert.That(rehydratedOrder.Assignee!.UserName, Is.EqualTo(CurrentUser.UserName));
	}
}

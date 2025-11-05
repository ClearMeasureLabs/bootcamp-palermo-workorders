using System.Diagnostics;
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
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var title = "Instructions Test - Max Length";
		var description = "Testing 4000 character instructions";
		var longInstructions = new string('X', 4000);
		var roomNumber = "101";

		await Input(nameof(WorkOrderManage.Elements.Title), title);
		await Input(nameof(WorkOrderManage.Elements.Description), description);
		await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var linkPattern = $"a[data-testid^='{nameof(WorkOrderSearch.Elements.WorkOrderLink)}']";
		var workOrderLink = Page.Locator(linkPattern).First;
		var workOrderNumberText = await workOrderLink.TextContentAsync();
		Debug.Assert(workOrderNumberText != null, "workOrderNumberText != null");

		await workOrderLink.ClickAsync();
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
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var title = "Instructions Test - Empty";
		var description = "Testing empty instructions";
		var roomNumber = "102";

		await Input(nameof(WorkOrderManage.Elements.Title), title);
		await Input(nameof(WorkOrderManage.Elements.Description), description);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var linkPattern = $"a[data-testid^='{nameof(WorkOrderSearch.Elements.WorkOrderLink)}']";
		var workOrderLink = Page.Locator(linkPattern).First;
		await workOrderLink.ClickAsync();
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("");
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

		var instructionsText = "These are detailed execution instructions for the work order";
		await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsText);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignCommand.Name);

		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(instructionsText);

		var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		Assert.That(rehydratedOrder.Instructions, Is.EqualTo(instructionsText));
	}
}

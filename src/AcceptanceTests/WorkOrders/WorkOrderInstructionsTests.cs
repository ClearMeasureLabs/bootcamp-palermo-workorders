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

		var longInstructions = new string('x', 4000);
		var order = await CreateAndSaveNewWorkOrder(instructions: longInstructions);

		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		var actualValue = await instructionsField.InputValueAsync();
		Assert.That(actualValue, Is.EqualTo(longInstructions));
		Assert.That(actualValue.Length, Is.EqualTo(4000));
	}

	[Test]
	public async Task ShouldCreateWorkOrderWithEmptyInstructions()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder(instructions: "");

		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("");
	}

	[Test]
	public async Task ShouldAddInstructionsAfterCreatingWorkOrder()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder(instructions: "");

		await Page.WaitForURLAsync("**/workorder/search");
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("");

		await Input(nameof(WorkOrderManage.Elements.Instructions), "Added instructions later");
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Expect(instructionsField).ToHaveValueAsync("Added instructions later");

		var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
		Assert.That(rehydratedOrder!.Instructions, Is.EqualTo("Added instructions later"));
	}
}

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

		var instructions4000 = new string('x', 4000);
		var order = await CreateAndSaveNewWorkOrder(instructions: instructions4000);

		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(instructions4000);

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe(instructions4000);
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

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe("");
	}

	[Test]
	public async Task ShouldSaveWorkOrderReturnLaterAddInstructionsAssignAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		// Create work order without instructions
		var order = await CreateAndSaveNewWorkOrder(instructions: "");

		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Navigate back to the work order
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Add instructions and assign
		var newInstructions = "Follow safety protocols when replacing bulbs";
		await Input(nameof(WorkOrderManage.Elements.Instructions), newInstructions);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Verify persistence by navigating back
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(newInstructions);

		var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
		await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

		// Verify via database query
		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe(newInstructions);
		rehydratedOrder.Assignee!.UserName.ShouldBe(CurrentUser.UserName);
	}
}

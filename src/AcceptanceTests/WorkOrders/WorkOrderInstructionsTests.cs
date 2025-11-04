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

		var longInstructions = new string('X', 4000);
		var order = await CreateAndSaveNewWorkOrder(longInstructions);

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(longInstructions);

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldNotBeNull();
		rehydratedOrder.Instructions!.Length.ShouldBe(4000);
	}

	[Test]
	public async Task ShouldCreateWorkOrderWithEmptyInstructions()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder("");

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("");

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe("");
	}

	[Test]
	public async Task ShouldAddInstructionsAfterInitialCreationAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder("");

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var newInstructions = "Follow safety protocol and use protective equipment";
		await Input(nameof(WorkOrderManage.Elements.Instructions), newInstructions);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(newInstructions);

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe(newInstructions);
		rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
	}
}

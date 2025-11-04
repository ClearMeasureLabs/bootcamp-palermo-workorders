using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
	[Test]
	public async Task ShouldDisplayInstructionsFieldWhenCreatingNewWorkOrder()
	{
		await LoginAsCurrentUser();
		await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToBeVisibleAsync();
		await Expect(instructionsField).ToBeEditableAsync();
	}

	[Test]
	public async Task ShouldSaveInstructionsWithNewWorkOrder()
	{
		await LoginAsCurrentUser();

		var order = Faker<WorkOrder>();
		order.Title = "Test Work Order";
		order.Instructions = "These are detailed instructions for completing this work order.";
		order.Number = null;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;

		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
		await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Instructions.ShouldBe(order.Instructions);
	}

	[Test]
	public async Task ShouldDisplayInstructionsWhenEditingWorkOrder()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();
		var testInstructions = "Updated instructions for this work order.";

		// Update the work order with instructions
		var existingOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
		existingOrder!.Instructions = testInstructions;
		await Bus.Send(new SaveDraftCommand(existingOrder));

		// Navigate to the work order
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(testInstructions);
	}

	[Test]
	public async Task ShouldUpdateInstructionsOnExistingWorkOrder()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var newInstructions = "These are the updated instructions.";
		await Input(nameof(WorkOrderManage.Elements.Instructions), newInstructions);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(newInstructions);
	}

	[Test]
	public async Task ShouldShowInstructionsAsReadOnlyWhenWorkOrderIsReadOnly()
	{
		await LoginAsCurrentUser();

		// Create and complete a work order
		var order = await CreateAndSaveNewWorkOrder();

		// Assign the work order
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await AssignExistingWorkOrder(order, CurrentUser.UserName);

		// Begin work
		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await BeginExistingWorkOrder(order);

		// Complete work
		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await CompleteExistingWorkOrder(order);

		// Navigate back to the completed work order
		await Page.WaitForURLAsync("**/workorder/search");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToBeDisabledAsync();
	}

	[Test]
	public async Task ShouldHandleEmptyInstructions()
	{
		await LoginAsCurrentUser();

		var order = Faker<WorkOrder>();
		order.Title = "Test Work Order Without Instructions";
		order.Instructions = "";
		order.Number = null;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;

		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
		await Input(nameof(WorkOrderManage.Elements.Instructions), "");
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Instructions.ShouldBe(string.Empty);
	}
}

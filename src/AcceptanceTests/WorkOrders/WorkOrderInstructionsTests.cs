using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
	[Test]
	public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
	{
		await LoginAsCurrentUser();

		var longInstructions = new string('X', 4000);
		var order = Faker<WorkOrder>();
		order.Title = "Test 4000 char instructions";
		order.Number = null;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;

		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), "Testing 4000 character instructions");
		await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		if (rehydratedOrder == null)
		{
			await Task.Delay(1000);
			rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		}
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Instructions.ShouldNotBeNull();
		rehydratedOrder.Instructions.Length.ShouldBe(4000);
	}

	[Test]
	public async Task ShouldCreateWorkOrderWithEmptyInstructions()
	{
		await LoginAsCurrentUser();

		var order = Faker<WorkOrder>();
		order.Title = "Test empty instructions";
		order.Number = null;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;

		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), "Testing empty instructions field");
		// Leave Instructions field empty
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		if (rehydratedOrder == null)
		{
			await Task.Delay(1000);
			rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		}
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Instructions.ShouldBe(string.Empty);
	}

	[Test]
	public async Task ShouldSaveWorkOrderReturnLaterAddInstructionsAssignAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		// Create initial work order without instructions
		var order = Faker<WorkOrder>();
		order.Title = "Test add instructions later";
		order.Number = null;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;

		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), "Initial description");
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Return to work order and add instructions
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsText = "Follow safety protocol. Wear protective equipment. Report completion.";
		await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsText);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Verify persistence
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(instructionsText);

		var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
		await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Instructions.ShouldBe(instructionsText);
		rehydratedOrder.Assignee.ShouldNotBeNull();
		rehydratedOrder.Assignee!.UserName.ShouldBe(CurrentUser.UserName);
	}
}

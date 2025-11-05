using System.Globalization;
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

		var order = Faker<WorkOrder>();
		order.Title = "Test 4000 char instructions";
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testInstructions = new string('X', 4000);
		var testRoomNumber = order.RoomNumber;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;
		await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
		await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
		await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

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
		rehydratedOrder.Instructions.ShouldBe(testInstructions);
		rehydratedOrder.Instructions!.Length.ShouldBe(4000);
	}

	[Test]
	public async Task ShouldCreateWorkOrderWithEmptyInstructions()
	{
		await LoginAsCurrentUser();

		var order = Faker<WorkOrder>();
		order.Title = "Test empty instructions";
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testRoomNumber = order.RoomNumber;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;
		await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
		await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
		// Leave Instructions empty
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

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
	public async Task ShouldSaveWorkOrderReturnAddInstructionsAssignAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		// Create work order without instructions
		var order = Faker<WorkOrder>();
		order.Title = "Test add instructions later";
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testRoomNumber = order.RoomNumber;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;
		await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
		await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
		// Leave Instructions empty initially
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

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

		// Return to the work order and add instructions
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var testInstructions = "New instructions added later";
		await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? finalOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		if (finalOrder == null)
		{
			await Task.Delay(1000);
			finalOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		}
		finalOrder.ShouldNotBeNull();
		finalOrder.Instructions.ShouldBe(testInstructions);
		finalOrder.Assignee.ShouldNotBeNull();
		finalOrder.Assignee!.UserName.ShouldBe(CurrentUser.UserName);
	}

	[Test]
	public async Task ShouldVerifyInstructionsFieldAppearsInUI()
	{
		await LoginAsCurrentUser();

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToBeVisibleAsync();
		await Expect(instructionsField).ToBeEditableAsync();
	}
}

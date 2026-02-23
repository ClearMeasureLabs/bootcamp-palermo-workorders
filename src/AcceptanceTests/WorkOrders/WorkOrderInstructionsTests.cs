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
		order.Title = "Test with max length instructions";
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testRoomNumber = order.RoomNumber;
		var testInstructions = new string('x', 4000);

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
		order.Title = "Test with empty instructions";
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
		// Intentionally leave Instructions empty
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
	public async Task ShouldAddInstructionsLaterAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		// Create work order without instructions
		var order = await CreateAndSaveNewWorkOrder();

		// Navigate back to work order
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		// Add instructions and assign
		var testInstructions = "Instructions added after initial save";
		await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Verify instructions persisted
		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Instructions.ShouldBe(testInstructions);

		// Navigate back to work order and verify in UI
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(testInstructions);
	}
}

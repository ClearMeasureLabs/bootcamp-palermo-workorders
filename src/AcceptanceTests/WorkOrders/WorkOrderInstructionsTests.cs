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
		var order = Faker<WorkOrder>();
		order.Title = "Work order with max instructions";
		order.Instructions = longInstructions;
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testInstructions = order.Instructions;
		var testRoomNumber = order.RoomNumber;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		await TakeScreenshotAsync(1, "NewWorkOrderPage");

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;

		await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
		await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
		await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
		await TakeScreenshotAsync(2, "FormFilledWith4000CharInstructions");

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
		order.Title = "Work order without instructions";
		order.Instructions = "";
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testRoomNumber = order.RoomNumber;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		await TakeScreenshotAsync(1, "NewWorkOrderPage");

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;

		await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
		await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
		await TakeScreenshotAsync(2, "FormFilledWithoutInstructions");

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
		rehydratedOrder.Instructions.ShouldBe("");
	}

	[Test]
	public async Task ShouldAddInstructionsToExistingWorkOrderAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var newInstructions = "Follow safety protocols and wear protective equipment";
		await Input(nameof(WorkOrderManage.Elements.Instructions), newInstructions);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(newInstructions);

		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Instructions.ShouldBe(newInstructions);
	}
}

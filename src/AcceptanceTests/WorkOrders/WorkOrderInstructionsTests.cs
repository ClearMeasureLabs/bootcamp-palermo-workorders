using System.Diagnostics;
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

		var order = Faker<WorkOrder>();
		order.Title = "Work order with max length instructions";
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testRoomNumber = order.RoomNumber;
		var testInstructions = new string('x', 4000);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		await TakeScreenshotAsync(1, "NewWorkOrderPage");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;
		await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
		await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
		await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
		await TakeScreenshotAsync(2, "FormFilledWithMaxInstructions");

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		if (rehyratedOrder == null)
		{
			await Task.Delay(1000);
			rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		}
		rehyratedOrder.ShouldNotBeNull();
		rehyratedOrder.Instructions.ShouldBe(testInstructions);
		rehyratedOrder.Instructions!.Length.ShouldBe(4000);
	}

	[Test]
	public async Task ShouldCreateWorkOrderWithEmptyInstructions()
	{
		await LoginAsCurrentUser();

		var order = Faker<WorkOrder>();
		order.Title = "Work order without instructions";
		order.Number = null;
		var testTitle = order.Title;
		var testDescription = order.Description;
		var testRoomNumber = order.RoomNumber;

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		await TakeScreenshotAsync(1, "NewWorkOrderPage");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
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

		WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		if (rehyratedOrder == null)
		{
			await Task.Delay(1000);
			rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		}
		rehyratedOrder.ShouldNotBeNull();
		rehyratedOrder.Instructions.ShouldBe(string.Empty);
	}

	[Test]
	public async Task ShouldSaveReturnLaterAddInstructionsAssignAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var newInstructions = "Follow proper safety procedures and wear protective equipment";
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
		await Input(nameof(WorkOrderManage.Elements.Instructions), newInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(newInstructions);

		WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehyratedOrder.Instructions.ShouldBe(newInstructions);
	}
}

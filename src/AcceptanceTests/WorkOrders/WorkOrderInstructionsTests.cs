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

		var longInstructions = new string('x', 4000);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

		await Input(nameof(WorkOrderManage.Elements.Title), "Test 4000 char instructions");
		await Input(nameof(WorkOrderManage.Elements.Description), "Testing long instructions");
		await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe(longInstructions);
		rehydratedOrder.Instructions!.Length.ShouldBe(4000);
	}

	[Test]
	public async Task ShouldCreateWorkOrderWithEmptyInstructions()
	{
		await LoginAsCurrentUser();

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");

		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

		await Input(nameof(WorkOrderManage.Elements.Title), "Test empty instructions");
		await Input(nameof(WorkOrderManage.Elements.Description), "Task without instructions");
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe(string.Empty);
	}

	[Test]
	public async Task ShouldSaveReturnAddInstructionsAssignAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsText = "Follow safety protocol. Use protective equipment. Report any issues immediately.";
		await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsText);
		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync(instructionsText);

		var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
		await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

		var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe(instructionsText);
	}
}

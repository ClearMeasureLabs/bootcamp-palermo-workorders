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
		await Input(nameof(WorkOrderManage.Elements.Description), "Testing maximum length instructions");
		await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? savedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
		if (savedOrder == null)
		{
			await Task.Delay(1000);
			savedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
		}

		savedOrder.ShouldNotBeNull();
		savedOrder.Instructions.ShouldNotBeNull();
		savedOrder.Instructions.Length.ShouldBe(4000);
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
		await Input(nameof(WorkOrderManage.Elements.Description), "Testing empty instructions field");

		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		WorkOrder? savedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
		if (savedOrder == null)
		{
			await Task.Delay(1000);
			savedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
		}

		savedOrder.ShouldNotBeNull();
		savedOrder.Title.ShouldBe("Test empty instructions");
		savedOrder.Description.ShouldBe("Testing empty instructions field");
	}

	[Test]
	public async Task ShouldSaveWorkOrderReturnLaterAddInstructionsAssignAndVerifyPersistence()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();

		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
		await Input(nameof(WorkOrderManage.Elements.Instructions), "New instructions added later");
		await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await woNumberLocator.WaitForAsync();
		await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

		var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
		await Expect(instructionsField).ToHaveValueAsync("New instructions added later");

		var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
		await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

		WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
		rehydratedOrder.Instructions.ShouldBe("New instructions added later");
		rehydratedOrder.Assignee.ShouldNotBeNull();
		rehydratedOrder.Assignee!.UserName.ShouldBe(CurrentUser.UserName);
	}
}

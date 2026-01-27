using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleValidationTests : AcceptanceTestBase
{
	[Test]
	public async Task ShouldRejectTitleWithNumbers()
	{
		await LoginAsCurrentUser();
		
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		
		await Input(nameof(WorkOrderManage.Elements.Title), "WorkOrder123");
		await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
		
		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		var validationMessage = Page.GetByText("Only letters are allowed");
		await Expect(validationMessage).ToBeVisibleAsync();
	}

	[Test]
	public async Task ShouldRejectTitleWithSpecialCharacters()
	{
		await LoginAsCurrentUser();
		
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		
		await Input(nameof(WorkOrderManage.Elements.Title), "Work Order! @#$");
		await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
		
		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		var validationMessage = Page.GetByText("Only letters are allowed");
		await Expect(validationMessage).ToBeVisibleAsync();
	}

	[Test]
	public async Task ShouldAcceptTitleWithOnlyLetters()
	{
		await LoginAsCurrentUser();
		
		var order = Faker<WorkOrder>();
		order.Title = "WorkOrderTitle";
		order.Number = null;
		
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		
		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;
		
		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);
		
		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		await Page.WaitForURLAsync("**/workorder/search");
		
		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		if (rehydratedOrder == null)
		{
			await Task.Delay(1000); 
			rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		}
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Title.ShouldBe(order.Title);
	}

	[Test]
	public async Task ShouldAcceptTitleWithMixedCase()
	{
		await LoginAsCurrentUser();
		
		var order = Faker<WorkOrder>();
		order.Title = "AbCdEfGh";
		order.Number = null;
		
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		
		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;
		
		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);
		
		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		await Page.WaitForURLAsync("**/workorder/search");
		
		WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		if (rehydratedOrder == null)
		{
			await Task.Delay(1000); 
			rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
		}
		rehydratedOrder.ShouldNotBeNull();
		rehydratedOrder.Title.ShouldBe(order.Title);
	}

	[Test]
	public async Task ShouldRejectTitleWithSpaces()
	{
		await LoginAsCurrentUser();
		
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		
		await Input(nameof(WorkOrderManage.Elements.Title), "Work Order Title");
		await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
		
		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		var validationMessage = Page.GetByText("Only letters are allowed");
		await Expect(validationMessage).ToBeVisibleAsync();
	}

	[Test]
	public async Task ShouldValidateExistingWorkOrderOnUpdate()
	{
		await LoginAsCurrentUser();
		
		var order = Faker<WorkOrder>();
		order.Title = "ValidTitle";
		order.Number = null;
		
		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		
		ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
		await Expect(woNumberLocator).ToBeVisibleAsync();
		var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
		order.Number = newWorkOrderNumber;
		
		await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
		await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
		await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);
		
		var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		await Page.WaitForURLAsync("**/workorder/search");
		
		await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		await Input(nameof(WorkOrderManage.Elements.Title), "Update123");
		await Click(saveButtonTestId);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		
		var validationMessage = Page.GetByText("Only letters are allowed");
		await Expect(validationMessage).ToBeVisibleAsync();
	}
}

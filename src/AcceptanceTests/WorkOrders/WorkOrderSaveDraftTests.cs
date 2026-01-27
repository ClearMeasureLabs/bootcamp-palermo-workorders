using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDraftTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldLoadScreenForNewWorkOrder()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
    }

    [Test]
    public async Task ShouldCreateNewWorkOrderAndVerifyOnSearchScreen()
    {
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "WorkOrderSearchAfterSave");

        Debug.Assert(order.Number != null, "order.Number != null");
        string orderNumber = order.Number;
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync($"/workorder/manage/{orderNumber}?mode=Edit");
        await TakeScreenshotAsync(5, "WorkOrderManagePage");

        var workOrderNumber = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(workOrderNumber).ToHaveTextAsync(orderNumber);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(order.Title!);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(order.Description!);

        var roomNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomNumberField).ToHaveValueAsync(order.RoomNumber!);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkOrderManage.Elements.CreatedDate));
        
        rehyratedOrder.CreatedDate.TruncateToMinute().ShouldBe(displayedDate);
    }

    [Test]
    public async Task ShouldAssignEmployeeAndSave()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "newtitle");
        await Input(nameof(WorkOrderManage.Elements.Description), "newdesc");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("newtitle");

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync("newdesc");

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkOrderManage.Elements.CreatedDate));
        
        rehyratedOrder.CreatedDate.TruncateToMinute().ShouldBe(displayedDate);
    }

    [Test]
    public async Task CreateWorkOrder_WithInstructions_SavesAndDisplaysCorrectly()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var title = "Work Order with Instructions";
        var description = "Test description";
        var instructions = new string('x', 100);
        var roomNumber = "Room 101";

        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrderSpecificationQuery());
        var order = orders.FirstOrDefault(o => o.Title == title);
        order.ShouldNotBeNull();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);
    }

    [Test]
    public async Task CreateWorkOrder_WithoutInstructions_SavesSuccessfully()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var title = "Work Order without Instructions";
        var description = "Test description";
        var roomNumber = "Room 102";

        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrderSpecificationQuery());
        var order = orders.FirstOrDefault(o => o.Title == title);
        order.ShouldNotBeNull();
        order.Instructions.ShouldBeNullOrEmpty();
    }

    [Test]
    public async Task EditWorkOrder_AddInstructions_PersistsChanges()
    {
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructions = new string('y', 200);
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(instructions);
    }

    [Test]
    public async Task EditWorkOrder_ModifyExistingInstructions_PersistsChanges()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var title = "Work Order with Initial Instructions";
        var description = "Test description";
        var initialInstructions = "Initial instructions text";
        var roomNumber = "Room 103";

        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), initialInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrderSpecificationQuery());
        var order = orders.FirstOrDefault(o => o.Title == title);
        order.ShouldNotBeNull();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var modifiedInstructions = "Modified instructions text";
        await Input(nameof(WorkOrderManage.Elements.Instructions), modifiedInstructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(modifiedInstructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(modifiedInstructions);
    }

    [Test]
    public async Task CreateWorkOrder_WithMaxLengthInstructions_SavesSuccessfully()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var title = "Work Order with Max Instructions";
        var description = "Test description";
        var maxInstructions = new string('z', 4000);
        var roomNumber = "Room 104";

        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), maxInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrderSpecificationQuery());
        var order = orders.FirstOrDefault(o => o.Title == title);
        order.ShouldNotBeNull();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(maxInstructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(maxInstructions);
        rehydratedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task WorkOrderDetailView_WithInstructions_DisplaysFieldBetweenDescriptionAndRoom()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var title = "Work Order for Layout Test";
        var description = "Test description for layout";
        var instructions = "Test instructions for layout";
        var roomNumber = "Room 105";

        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrderSpecificationQuery());
        var order = orders.FirstOrDefault(o => o.Title == title);
        order.ShouldNotBeNull();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var roomNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));

        await Expect(descriptionField).ToBeVisibleAsync();
        await Expect(instructionsField).ToBeVisibleAsync();
        await Expect(roomNumberField).ToBeVisibleAsync();

        await Expect(instructionsField).ToHaveValueAsync(instructions);
    }
}
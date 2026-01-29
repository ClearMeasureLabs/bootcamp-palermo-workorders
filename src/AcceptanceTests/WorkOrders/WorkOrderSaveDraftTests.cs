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
    public async Task CreateWorkOrder_With300CharacterTitle_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var title300 = new string('T', 300);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), title300);
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder!.Title.ShouldBe(title300);
        rehyratedOrder.Title!.Length.ShouldBe(300);
    }

    [Test]
    public async Task CreateWorkOrder_With301CharacterTitle_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        var title301 = new string('T', 301);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), title301);
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        
        // Should still be on the manage page due to validation error
        await Expect(Page).ToHaveURLAsync("**/workorder/manage?mode=New");
        
        // Verify validation error message is displayed
        var validationSummary = Page.Locator(".validation-message, .validation-summary-errors");
        await Expect(validationSummary).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditWorkOrder_UpdateTitleTo300Characters_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        var title300 = new string('U', 300);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Input(nameof(WorkOrderManage.Elements.Title), title300);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehyratedOrder.Title.ShouldBe(title300);
        rehyratedOrder.Title!.Length.ShouldBe(300);
    }

    [Test]
    public async Task EditWorkOrder_UpdateTitleTo301Characters_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        var title301 = new string('U', 301);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), title301);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        // Should still be on the edit page due to validation error
        await Expect(Page).ToHaveURLAsync($"**/workorder/manage/{order.Number}?mode=Edit");
        
        // Verify validation error message is displayed
        var validationSummary = Page.Locator(".validation-message, .validation-summary-errors");
        await Expect(validationSummary).ToBeVisibleAsync();
        
        // Verify title was not updated in database
        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehyratedOrder.Title.ShouldNotBe(title301);
    }
}
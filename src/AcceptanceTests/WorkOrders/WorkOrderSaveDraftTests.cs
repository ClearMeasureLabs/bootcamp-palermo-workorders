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
    public async Task ShouldDisplayRequiredAsterisksOnTitleAndDescriptionFields()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var titleLabel = Page.Locator("label[for='Title']");
        await Expect(titleLabel).ToContainTextAsync("*");

        var descriptionLabel = Page.Locator("label[for='Description']");
        await Expect(descriptionLabel).ToContainTextAsync("*");
    }

    [Test]
    public async Task ShouldShowValidationModalWhenCreatingWorkOrderWithEmptyTitle()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("Title");
    }

    [Test]
    public async Task ShouldShowValidationModalWhenCreatingWorkOrderWithEmptyDescription()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("Description");
    }

    [Test]
    public async Task ShouldShowValidationModalWhenCreatingWorkOrderWithBothFieldsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("Title");
        await Expect(validationSummary).ToContainTextAsync("Description");
    }

    [Test]
    public async Task ShouldSuccessfullyCreateWorkOrderWithValidTitleAndDescription()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Debug.Assert(order.Number != null, "order.Number != null");
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(order.Title!);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(order.Description!);
    }

    [Test]
    public async Task ShouldShowValidationModalWhenClearingTitleOnExistingWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("Title");
    }

    [Test]
    public async Task ShouldShowValidationModalWhenClearingDescriptionOnExistingWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Description), "");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("Description");
    }

    [Test]
    public async Task ShouldSuccessfullyUpdateWorkOrderWithValidTitleAndDescription()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Updated title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Updated description");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("Updated title");

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync("Updated description");
    }
}
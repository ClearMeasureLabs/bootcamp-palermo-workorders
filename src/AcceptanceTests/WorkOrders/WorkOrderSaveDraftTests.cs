using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDraftTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldLoadScreenForNewWorkOrder()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
    }

    [Test, Retry(2)]
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

    [Test, Retry(2)]
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

    [Test, Retry(2)]
    public async Task CharacterCounterDisplaysForTitleField()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleCharCounter = Page.GetByTestId(nameof(WorkOrderManage.Elements.TitleCharCounter));
        await Expect(titleCharCounter).ToBeVisibleAsync();
        await Expect(titleCharCounter).ToContainTextAsync("0/250 characters");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Expect(titleCharCounter).ToContainTextAsync("10/250 characters");
    }

    [Test, Retry(2)]
    public async Task CharacterCounterDisplaysForDescriptionField()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var descCharCounter = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCounter));
        await Expect(descCharCounter).ToBeVisibleAsync();
        await Expect(descCharCounter).ToContainTextAsync("0/500 characters");

        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Expect(descCharCounter).ToContainTextAsync("16/500 characters");
    }

    [Test, Retry(2)]
    public async Task TitleMaxlengthAttributePreventsInputBeyondLimit()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var longTitle = new string('A', 260);
        await Input(nameof(WorkOrderManage.Elements.Title), longTitle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        var actualValue = await titleField.InputValueAsync();
        
        actualValue.Length.ShouldBeLessThanOrEqualTo(250);
    }

    [Test, Retry(2)]
    public async Task DescriptionMaxlengthAttributePreventsInputBeyondLimit()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var longDesc = new string('B', 510);
        await Input(nameof(WorkOrderManage.Elements.Description), longDesc);

        var descField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var actualValue = await descField.InputValueAsync();
        
        actualValue.Length.ShouldBeLessThanOrEqualTo(500);
    }

    [Test, Retry(2)]
    public async Task CharacterCounterWarningStateWhenApproachingTitleLimit()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleApproachingLimit = new string('A', 226);
        await Input(nameof(WorkOrderManage.Elements.Title), titleApproachingLimit);

        var titleCharCounter = Page.GetByTestId(nameof(WorkOrderManage.Elements.TitleCharCounter));
        await Expect(titleCharCounter).ToHaveClassAsync(new Regex("char-counter-warning"));
    }

    [Test, Retry(2)]
    public async Task CharacterCounterWarningStateWhenApproachingDescriptionLimit()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var descApproachingLimit = new string('B', 451);
        await Input(nameof(WorkOrderManage.Elements.Description), descApproachingLimit);

        var descCharCounter = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCounter));
        await Expect(descCharCounter).ToHaveClassAsync(new Regex("char-counter-warning"));
    }
}
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_With700CharacterTitle_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var title700 = new string('T', 700);
        var description = "Testing 700 character title";
        var roomNumber = "101";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), title700);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);
        await TakeScreenshotAsync(2, "FormFilled");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? workOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        if (workOrder == null)
        {
            await Task.Delay(1000);
            workOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        }
        workOrder.ShouldNotBeNull();
        workOrder.Title.ShouldBe(title700);
        workOrder.Title.Length.ShouldBe(700);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleLocator).ToHaveValueAsync(title700);
    }

    [Test]
    public async Task EditWorkOrder_ExtendTitleTo700Characters_UpdatesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        var title700 = new string('U', 700);
        await Input(nameof(WorkOrderManage.Elements.Title), title700);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? updatedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        if (updatedOrder == null)
        {
            await Task.Delay(1000);
            updatedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        }
        updatedOrder.ShouldNotBeNull();
        updatedOrder.Title.ShouldBe(title700);
        updatedOrder.Title.Length.ShouldBe(700);

        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleLocator).ToHaveValueAsync(title700);
    }

    [Test]
    public async Task ViewWorkOrder_With700CharacterTitle_DisplaysCompleteTitle()
    {
        await LoginAsCurrentUser();

        var title700 = new string('V', 700);
        var description = "Testing viewing 700 character title";
        var roomNumber = "102";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), title700);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleLocator).ToBeVisibleAsync();
        var displayedTitle = await titleLocator.InputValueAsync();
        displayedTitle.ShouldBe(title700);
        displayedTitle.Length.ShouldBe(700);
    }

    [Test]
    public async Task CreateWorkOrder_TitleExceeds700Characters_HandlesGracefully()
    {
        await LoginAsCurrentUser();

        var title750 = new string('X', 750);
        var description = "Testing title exceeding 700 characters";
        var roomNumber = "103";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), title750);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), roomNumber);

        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        var actualTitle = await titleLocator.InputValueAsync();
        
        // Verify that the input was limited/truncated to 700 characters
        actualTitle.Length.ShouldBeLessThanOrEqualTo(700);
        
        // Attempt to save - should either succeed with truncated title or show validation
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // If saved, verify the persisted title is at most 700 characters
        WorkOrder? workOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        if (workOrder != null)
        {
            workOrder.Title.Length.ShouldBeLessThanOrEqualTo(700);
        }
    }
}

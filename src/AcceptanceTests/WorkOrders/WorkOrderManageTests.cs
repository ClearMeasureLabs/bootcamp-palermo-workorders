using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_WithMaximumTitleLength_Succeeds()
    {
        await LoginAsCurrentUser();

        var longTitle = new string('x', 650);
        var testDescription = "Test description for long title";
        var testRoomNumber = "ROOM-123";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), longTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
        await TakeScreenshotAsync(2, "FormFilledWithLongTitle");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        if (rehyratedOrder == null)
        {
            await Task.Delay(1000);
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        }
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Title.ShouldBe(longTitle);
        rehyratedOrder.Title!.Length.ShouldBe(650);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "WorkOrderDetailPage");

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(longTitle);
    }

    [Test]
    public async Task EditWorkOrder_ExtendTitleToMaximumLength_Succeeds()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order.Title!.Length.ShouldBeLessThan(650);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var longTitle = new string('y', 650);
        await Input(nameof(WorkOrderManage.Elements.Title), longTitle);
        await TakeScreenshotAsync(1, "UpdatedWithLongTitle");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? updatedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        updatedOrder.ShouldNotBeNull();
        updatedOrder.Title.ShouldBe(longTitle);
        updatedOrder.Title!.Length.ShouldBe(650);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(longTitle);
    }

    [Test]
    public async Task ViewWorkOrder_WithLongTitle_DisplaysCorrectly()
    {
        await LoginAsCurrentUser();

        var longTitle = new string('z', 650);
        var testDescription = "Test description";
        var testRoomNumber = "ROOM-456";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), longTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "LongTitleDisplay");

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(longTitle);
        
        var actualValue = await titleField.InputValueAsync();
        actualValue.Length.ShouldBe(650);
    }

    [Test]
    public async Task ListWorkOrders_WithLongTitles_DisplaysCorrectly()
    {
        await LoginAsCurrentUser();

        var shortTitle = "Short title";
        var mediumTitle = new string('m', 300);
        var longTitle = new string('l', 650);

        await CreateWorkOrderWithTitle(shortTitle);
        await CreateWorkOrderWithTitle(mediumTitle);
        await CreateWorkOrderWithTitle(longTitle);

        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "WorkOrderListWithVariousLengths");

        var searchResults = Page.Locator("[data-testid^='WorkOrderLink']");
        var count = await searchResults.CountAsync();
        count.ShouldBeGreaterThanOrEqualTo(3);
    }

    private async Task<WorkOrder> CreateWorkOrderWithTitle(string title)
    {
        var order = Faker<WorkOrder>();
        order.Title = title;
        order.Number = null;
        var testDescription = "Test description";
        var testRoomNumber = "TEST-ROOM";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

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
        return rehyratedOrder;
    }
}

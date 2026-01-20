using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_With300CharacterTitle_SuccessfullyCreatesAndDisplays()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var orderNumber = await woNumberLocator.InnerTextAsync();

        var title300 = new string('A', 300);
        await Input(nameof(WorkOrderManage.Elements.Title), title300);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "ROOM-300");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 300 character title");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(title300);
        
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber));
        rehydratedOrder!.Title!.Length.ShouldBe(300);
    }

    [Test]
    public async Task CreateWorkOrder_With301CharacterTitle_FailsToSave()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var orderNumber = await woNumberLocator.InnerTextAsync();

        var title301 = new string('B', 301);
        await Input(nameof(WorkOrderManage.Elements.Title), title301);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "ROOM-301");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 301 character title");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(2000);
        
        // Should still be on the manage page after failed save
        await Expect(Page).ToHaveURLAsync("**/workorder/manage?mode=New");
        
        // Order should not exist in database
        var orderQuery = await Bus.Send(new WorkOrderByNumberQuery(orderNumber));
        orderQuery.ShouldBeNull();
    }

    [Test]
    public async Task EditWorkOrder_ChangeTitleTo300Characters_SuccessfullySaves()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var title300 = new string('C', 300);
        await Input(nameof(WorkOrderManage.Elements.Title), title300);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(title300);
        
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder!.Title!.Length.ShouldBe(300);
    }

    [Test]
    public async Task EditWorkOrder_ChangeTitleTo301Characters_FailsToSave()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        var originalTitle = order.Title;

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var title301 = new string('D', 301);
        await Input(nameof(WorkOrderManage.Elements.Title), title301);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(2000);
        
        // Should still be on edit page after failed save
        await Expect(Page).ToHaveURLAsync($"**/workorder/manage/{order.Number}?mode=Edit");

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder!.Title.ShouldBe(originalTitle);
    }

    [Test]
    public async Task CreateWorkOrder_With299CharacterTitle_SuccessfullyCreates()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var orderNumber = await woNumberLocator.InnerTextAsync();

        var title299 = new string('E', 299);
        await Input(nameof(WorkOrderManage.Elements.Title), title299);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "ROOM-299");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 299 character title");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(orderNumber));
        rehydratedOrder!.Title.ShouldBe(title299);
        rehydratedOrder.Title!.Length.ShouldBe(299);
    }
}

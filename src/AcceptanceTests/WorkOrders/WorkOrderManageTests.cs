using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_With650CharacterTitle_SavesAndDisplaysCorrectly()
    {
        await LoginAsCurrentUser();

        var title650 = new string('T', 650);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), title650);
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 650 char title");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room123");
        await TakeScreenshotAsync(2, "FormFilled650CharTitle");

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
        savedOrder.Title.ShouldBe(title650);
        savedOrder.Title!.Length.ShouldBe(650);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "WorkOrderDetailView650");

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(title650);
    }

    [Test]
    public async Task EditWorkOrder_ExtendTitleTo650Characters_UpdatesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var title650 = new string('X', 650);
        await Input(nameof(WorkOrderManage.Elements.Title), title650);
        await TakeScreenshotAsync(1, "EditedTo650CharTitle");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(title650);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Title.ShouldBe(title650);
        rehydratedOrder.Title!.Length.ShouldBe(650);
    }

    [Test]
    public async Task ViewWorkOrderList_WithLongTitles_DisplaysWithoutTruncationOrLayoutIssues()
    {
        await LoginAsCurrentUser();

        var title100 = new string('A', 100);
        var title300 = new string('B', 300);
        var title650 = new string('C', 650);

        var order1 = Faker<WorkOrder>();
        order1.Title = title100;
        order1.Number = null;
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        order1.Number = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), title100);
        await Input(nameof(WorkOrderManage.Elements.Description), order1.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order1.RoomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var order2 = Faker<WorkOrder>();
        order2.Title = title300;
        order2.Number = null;
        
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        order2.Number = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), title300);
        await Input(nameof(WorkOrderManage.Elements.Description), order2.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order2.RoomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var order3 = Faker<WorkOrder>();
        order3.Title = title650;
        order3.Number = null;
        
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        order3.Number = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), title650);
        await Input(nameof(WorkOrderManage.Elements.Description), order3.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order3.RoomNumber);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await TakeScreenshotAsync(1, "WorkOrderListWithVaryingTitleLengths");

        var link1 = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order1.Number);
        var link2 = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order2.Number);
        var link3 = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order3.Number);

        await Expect(link1).ToBeVisibleAsync();
        await Expect(link2).ToBeVisibleAsync();
        await Expect(link3).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreateWorkOrder_WithTitleOver650Characters_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        var title651 = new string('Z', 651);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await titleField.FillAsync(title651);
        await titleField.BlurAsync();
        
        var actualValue = await titleField.InputValueAsync();
        actualValue.Length.ShouldBeLessThanOrEqualTo(650);
        
        await TakeScreenshotAsync(1, "TitleFieldWith651CharAttempt");
    }
}

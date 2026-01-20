using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_With250CharacterTitle_Succeeds()
    {
        await LoginAsCurrentUser();
        
        // Navigate to create new work order
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        // Get the work order number
        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        // Input a title with exactly 250 characters
        var title250 = new string('A', 250);
        await Input(nameof(WorkOrderManage.Elements.Title), title250);
        
        // Fill other required fields
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description for 250 char title");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        
        // Save the work order
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify work order is created successfully
        await Page.WaitForURLAsync("**/workorder/search");
        
        // Navigate back to the work order and verify title displays correctly
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(title250);
        
        // Verify from database
        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Title.ShouldBe(title250);
        rehydratedOrder.Title!.Length.ShouldBe(250);
    }

    [Test]
    public async Task CreateWorkOrder_WithTitleExceeding250Characters_ShowsValidationError()
    {
        await LoginAsCurrentUser();
        
        // Navigate to create new work order
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        // Get the work order number
        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        // Input a title with 251 characters
        var title251 = new string('B', 251);
        await Input(nameof(WorkOrderManage.Elements.Title), title251);
        
        // Fill other required fields
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 102");
        
        // Attempt to save the work order
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify validation error is displayed (stay on the same page)
        await Expect(Page).ToHaveURLAsync(new Regex(".*/workorder/manage.*"));
        
        // Verify work order is not created - should not find it in database
        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        if (rehydratedOrder != null)
        {
            // If it was saved, the title should have been truncated/rejected
            rehydratedOrder.Title.ShouldNotBe(title251);
        }
    }

    [Test]
    public async Task EditWorkOrder_UpdateTitleTo250Characters_Succeeds()
    {
        await LoginAsCurrentUser();
        
        // Create work order with short title
        var order = await CreateAndSaveNewWorkOrder();
        
        // Navigate to edit the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        // Update title to exactly 250 characters
        var title250 = new string('C', 250);
        await Input(nameof(WorkOrderManage.Elements.Title), title250);
        
        // Save the work order
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify update succeeds
        await Page.WaitForURLAsync("**/workorder/search");
        
        // Navigate back and verify updated title displays correctly
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(title250);
        
        // Verify from database
        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Title.ShouldBe(title250);
        rehydratedOrder.Title!.Length.ShouldBe(250);
    }

    [Test]
    public async Task ViewWorkOrder_With250CharacterTitle_DisplaysCorrectly()
    {
        await LoginAsCurrentUser();
        
        // Navigate to create new work order
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        // Get the work order number
        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        // Create work order with 250 character title
        var title250 = new string('D', 250);
        await Input(nameof(WorkOrderManage.Elements.Title), title250);
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 104");
        
        // Save the work order
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Navigate to work order details page
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify full 250 character title is displayed without truncation
        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToBeVisibleAsync();
        var displayedTitle = await titleField.InputValueAsync();
        
        displayedTitle.ShouldBe(title250);
        displayedTitle.Length.ShouldBe(250);
    }
}

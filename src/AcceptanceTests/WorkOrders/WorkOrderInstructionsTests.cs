using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var longInstructions = new string('x', 4000);
        var order = Faker<WorkOrder>();
        order.Title = "Test 4000 char instructions";
        order.Number = null;

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldNotBeNull();
        rehydratedOrder.Instructions.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test empty instructions";
        order.Number = null;

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        // Leave Instructions empty
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Title.ShouldBe(order.Title);
    }

    [Test]
    public async Task ShouldAddInstructionsAfterInitialSaveAndAssign()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test add instructions later";
        order.Number = null;

        // Create work order without instructions
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add instructions and assign
        var testInstructions = "Follow these steps carefully: 1. Inspect the area. 2. Perform repairs. 3. Test the fix.";
        await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back and verify persistence
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(testInstructions);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe(testInstructions);
        rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }
}

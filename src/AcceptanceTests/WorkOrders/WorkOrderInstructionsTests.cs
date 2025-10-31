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

        var order = Faker<WorkOrder>();
        order.Title = "Work order with max instructions";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var longInstructions = new string('x', 4000);
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        if (rehydratedOrder == null)
        {
            await Task.Delay(1000);
            rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        }

        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe(longInstructions);
        rehydratedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Work order without instructions";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        if (rehydratedOrder == null)
        {
            await Task.Delay(1000);
            rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        }

        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldAddInstructionsLaterAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var instructionsToAdd = "Follow these safety procedures: 1. Turn off power 2. Wear protective gear 3. Check for hazards";
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsToAdd);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructionsToAdd);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe(instructionsToAdd);
    }

    [Test]
    public async Task ShouldVerifyInstructionsFieldExistsAndIsEditable()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToBeVisibleAsync();
        await Expect(instructionsField).ToBeEditableAsync();

        var testInstructions = "Test instructions content";
        await instructionsField.FillAsync(testInstructions);
        await Expect(instructionsField).ToHaveValueAsync(testInstructions);
    }
}

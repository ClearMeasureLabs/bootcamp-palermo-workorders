using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var longInstructions = new string('X', 4000);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe(longInstructions);
        rehydratedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldSaveReturnAddInstructionsAssignAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order");
        await Input(nameof(WorkOrderManage.Elements.Description), "Initial Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "103");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(newWorkOrderNumber);

        await Input(nameof(WorkOrderManage.Elements.Instructions), "New instructions added later");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);

        var assignButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name;
        await Click(assignButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("New instructions added later");

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe("New instructions added later");
    }
}

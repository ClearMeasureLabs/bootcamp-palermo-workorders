using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
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

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var longInstructions = new string('x', 4000);

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Long Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 4000 character instructions");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrders = await Bus.Send(new WorkOrderAllQuery());
        var workOrder = workOrders.OrderByDescending(x => x.CreatedDate).First();

        workOrder.Instructions.ShouldNotBeNull();
        workOrder.Instructions.Length.ShouldBe(4000);
        workOrder.Instructions.ShouldBe(longInstructions);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Empty Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing empty instructions field");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrders = await Bus.Send(new WorkOrderAllQuery());
        var workOrder = workOrders.OrderByDescending(x => x.CreatedDate).First();

        workOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldSaveAndReturnToAddInstructionsAndAssign()
    {
        await LoginAsCurrentUser();

        // Create work order without instructions
        var order = await CreateAndSaveNewWorkOrder();

        // Navigate back to the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add instructions and assign
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Follow safety procedures carefully");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify instructions were saved
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe("Follow safety procedures carefully");
        rehydratedOrder.Assignee.ShouldNotBeNull();
        rehydratedOrder.Assignee!.UserName.ShouldBe(CurrentUser.UserName);

        // Navigate back and verify UI shows instructions
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("Follow safety procedures carefully");
    }

    [Test]
    public async Task ShouldUpdateExistingInstructions()
    {
        await LoginAsCurrentUser();

        // Create work order with initial instructions
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Update Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing updating instructions");
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Initial instructions");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "103");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrders = await Bus.Send(new WorkOrderAllQuery());
        var workOrder = workOrders.OrderByDescending(x => x.CreatedDate).First();

        // Navigate back and update instructions
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrder.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Instructions), "Updated instructions with more details");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify instructions were updated
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(workOrder.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe("Updated instructions with more details");
    }
}

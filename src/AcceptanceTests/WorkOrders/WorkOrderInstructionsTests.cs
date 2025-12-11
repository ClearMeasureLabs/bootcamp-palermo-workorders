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

        var longInstructions = new string('X', 4000);
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test 4000 char instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing instructions field with max length");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var allOrders = await Bus.Send(new WorkOrdersListAllQuery());
        var createdOrder = allOrders.OrderByDescending(o => o.CreatedDate).First();
        
        createdOrder.Instructions.ShouldNotBeNull();
        createdOrder.Instructions.Length.ShouldBe(4000);
        createdOrder.Instructions.ShouldBe(longInstructions);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test empty instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing instructions field when empty");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var allOrders = await Bus.Send(new WorkOrdersListAllQuery());
        var createdOrder = allOrders.OrderByDescending(o => o.CreatedDate).First();
        
        createdOrder.Instructions.ShouldNotBeNull();
        createdOrder.Instructions.ShouldBe("");
    }

    [Test]
    public async Task ShouldAddInstructionsAfterSavingAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        // Create work order without instructions
        var order = await CreateAndSaveNewWorkOrder();
        order.Instructions.ShouldBe("");

        // Navigate to edit the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add instructions
        var instructions = "Follow safety procedures when working";
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        
        // Assign the work order
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify instructions persisted
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldBe(instructions);

        // Navigate back to the work order and verify in UI
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);
    }

    [Test]
    public async Task ShouldUpdateExistingInstructions()
    {
        await LoginAsCurrentUser();

        // Create work order with initial instructions
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test update instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing instructions update");
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Initial instructions");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "103");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var allOrders = await Bus.Send(new WorkOrdersListAllQuery());
        var createdOrder = allOrders.OrderByDescending(o => o.CreatedDate).First();
        createdOrder.Instructions.ShouldBe("Initial instructions");

        // Edit and update instructions
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + createdOrder.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Instructions), "Updated instructions");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify updated instructions
        var updatedOrder = await Bus.Send(new WorkOrderByNumberQuery(createdOrder.Number!));
        updatedOrder.ShouldNotBeNull();
        updatedOrder.Instructions.ShouldBe("Updated instructions");
    }
}

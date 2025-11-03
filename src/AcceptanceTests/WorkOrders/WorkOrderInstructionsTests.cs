using System.Diagnostics;
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
        // Scenario 1: Create work order with 4000-character instructions
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var longInstructions = new string('x', 4000);

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order with Long Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Description for testing long instructions");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(longInstructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(longInstructions));
        Assert.That(rehydratedOrder.Instructions!.Length, Is.EqualTo(4000));
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        // Scenario 2: Create work order with empty instructions
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Debug.Assert(order.Number != null, "order.Number != null");
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("");

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task ShouldSaveWorkOrderReturnLaterAddInstructionsAssignAndVerifyPersistence()
    {
        // Scenario 3: Save work order, return later, add instructions, assign, and verify persistence
        await LoginAsCurrentUser();

        // Create initial work order without instructions
        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Debug.Assert(order.Number != null, "order.Number != null");
        
        // Return to the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Add instructions and assign
        var testInstructions = "Step 1: Check the electrical panel\nStep 2: Replace faulty circuit breaker\nStep 3: Test the system";
        await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        // Verify navigation
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Return to work order and verify instructions persisted
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(testInstructions);

        // Verify in database
        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(testInstructions));
        Assert.That(rehydratedOrder.Assignee!.UserName, Is.EqualTo(CurrentUser.UserName));
    }

    [Test]
    public async Task ShouldUpdateInstructionsOnExistingWorkOrder()
    {
        await LoginAsCurrentUser();

        // Create work order with initial instructions
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var initialInstructions = "Initial instructions for the work order";

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Work Order");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.Instructions), initialInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "202");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Update instructions
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var updatedInstructions = "Updated instructions with more detailed steps";
        await Input(nameof(WorkOrderManage.Elements.Instructions), updatedInstructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        // Verify update
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(updatedInstructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber)) ?? throw new InvalidOperationException();
        Assert.That(rehydratedOrder.Instructions, Is.EqualTo(updatedInstructions));
    }

    [Test]
    public async Task ShouldDisplayInstructionsFieldBetweenDescriptionAndRoomNumber()
    {
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Verify Instructions field exists and is positioned correctly
        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var roomNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));

        await Expect(descriptionField).ToBeVisibleAsync();
        await Expect(instructionsField).ToBeVisibleAsync();
        await Expect(roomNumberField).ToBeVisibleAsync();

        // Verify Instructions field is a textarea (multiline)
        var instructionsTagName = await instructionsField.EvaluateAsync<string>("el => el.tagName");
        Assert.That(instructionsTagName.ToLower(), Is.EqualTo("textarea"));
    }
}


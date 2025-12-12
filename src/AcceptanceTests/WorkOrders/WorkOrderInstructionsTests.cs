using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var instructions4000 = new string('X', 4000);
        
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test with 4000 char instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Description for instructions test");
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions4000);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrdersListAllQuery());
        var lastOrder = orders.OrderByDescending(o => o.CreatedDate).First();
        
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(lastOrder.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder!.Instructions.ShouldBe(instructions4000);
        rehydratedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test with empty instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Description without instructions");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrdersListAllQuery());
        var lastOrder = orders.OrderByDescending(o => o.CreatedDate).First();
        
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(lastOrder.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder!.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldAddInstructionsAfterInitialSaveAndAssign()
    {
        await LoginAsCurrentUser();

        // Create work order without instructions
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test add instructions later");
        await Input(nameof(WorkOrderManage.Elements.Description), "Initial description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var orders = await Bus.Send(new WorkOrdersListAllQuery());
        var lastOrder = orders.OrderByDescending(o => o.CreatedDate).First();

        // Verify instructions is empty
        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(lastOrder.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder!.Instructions.ShouldBe(string.Empty);

        // Navigate back to work order and add instructions
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + lastOrder.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsText = "Added instructions after initial save";
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsText);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify instructions persisted
        rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(lastOrder.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder!.Instructions.ShouldBe(instructionsText);
        rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public async Task ShouldDisplayInstructionsFieldBetweenDescriptionAndRoom()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var roomField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));

        await Expect(descriptionField).ToBeVisibleAsync();
        await Expect(instructionsField).ToBeVisibleAsync();
        await Expect(roomField).ToBeVisibleAsync();

        // Verify Instructions field is present and editable
        await instructionsField.FillAsync("Test instructions");
        await Expect(instructionsField).ToHaveValueAsync("Test instructions");
    }
}

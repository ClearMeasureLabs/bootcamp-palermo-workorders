using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_WithInstructions_SavesAndDisplaysCorrectly()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test with Instructions";
        order.Description = "Test description";
        order.Instructions = "Follow these specific instructions carefully";
        order.RoomNumber = "101";
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(order.Instructions);
    }

    [Test]
    public async Task CreateWorkOrder_WithoutInstructions_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test without Instructions";
        order.Description = "Test description";
        order.RoomNumber = "102";
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Title.ShouldBe(order.Title);
    }

    [Test]
    public async Task EditWorkOrder_UpdateInstructions_ChangesPersist()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order.Instructions = "Initial instructions";

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(order.Instructions);

        var updatedInstructions = "Updated instructions text";
        await Input(nameof(WorkOrderManage.Elements.Instructions), updatedInstructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(updatedInstructions);
    }

    [Test]
    public async Task CreateWorkOrder_WithMaxLengthInstructions_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test with max length Instructions";
        order.Description = "Test description";
        order.Instructions = new string('x', 4000);
        order.RoomNumber = "103";
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Instructions.ShouldNotBeNull();
        rehydratedOrder.Instructions.Length.ShouldBe(4000);
    }

    [Test]
    public async Task WorkOrderDetails_WithInstructions_DisplaysInCorrectPosition()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test field position";
        order.Description = "Test description";
        order.Instructions = "Test instructions";
        order.RoomNumber = "104";
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var roomNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));

        await Expect(descriptionField).ToBeVisibleAsync();
        await Expect(instructionsField).ToBeVisibleAsync();
        await Expect(roomNumberField).ToBeVisibleAsync();

        var descriptionBox = await descriptionField.BoundingBoxAsync();
        var instructionsBox = await instructionsField.BoundingBoxAsync();
        var roomBox = await roomNumberField.BoundingBoxAsync();

        descriptionBox.ShouldNotBeNull();
        instructionsBox.ShouldNotBeNull();
        roomBox.ShouldNotBeNull();

        (descriptionBox.Y < instructionsBox.Y).ShouldBeTrue();
        (instructionsBox.Y < roomBox.Y).ShouldBeTrue();
    }
}

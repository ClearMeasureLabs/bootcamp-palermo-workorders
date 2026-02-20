using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_WithInstructions_SavesAndDisplaysInstructions()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test with instructions";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var testInstructions = "Follow these step-by-step instructions carefully";
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
        await TakeScreenshotAsync(2, "FormFilledWithInstructions");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        if (rehyratedOrder == null)
        {
            await Task.Delay(1000);
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        }
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Instructions.ShouldBe(testInstructions);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(testInstructions);
        await TakeScreenshotAsync(3, "WorkOrderDisplaysInstructions");
    }

    [Test]
    public async Task CreateWorkOrder_WithoutInstructions_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test without instructions";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        // Leaving Instructions blank
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        if (rehyratedOrder == null)
        {
            await Task.Delay(1000);
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        }
        rehyratedOrder.ShouldNotBeNull();
        await TakeScreenshotAsync(1, "WorkOrderSavedWithoutInstructions");
    }

    [Test]
    public async Task EditWorkOrder_AddInstructions_PersistsChanges()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var newInstructions = "Added instructions after initial save";
        await Input(nameof(WorkOrderManage.Elements.Instructions), newInstructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(newInstructions);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehyratedOrder.Instructions.ShouldBe(newInstructions);
        await TakeScreenshotAsync(1, "InstructionsAddedAfterSave");
    }

    [Test]
    public async Task EditWorkOrder_ModifyInstructions_PersistsChanges()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test modify instructions";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var initialInstructions = "Initial instructions text";
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.Instructions), initialInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var modifiedInstructions = "Modified instructions with updated details";
        await Input(nameof(WorkOrderManage.Elements.Instructions), modifiedInstructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(modifiedInstructions);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehyratedOrder.Instructions.ShouldBe(modifiedInstructions);
        await TakeScreenshotAsync(1, "InstructionsModified");
    }

    [Test]
    public async Task CreateWorkOrder_WithMaximumCharacterInstructions_SavesSuccessfully()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test max char instructions";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var maxInstructions = new string('X', 4000);
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.Instructions), maxInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        if (rehyratedOrder == null)
        {
            await Task.Delay(1000);
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        }
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Instructions.ShouldNotBeNull();
        rehyratedOrder.Instructions!.Length.ShouldBe(4000);
        await TakeScreenshotAsync(1, "MaxCharInstructionsSaved");
    }

    [Test]
    public async Task WorkOrderForm_InstructionsFieldPlacement_DisplaysBetweenDescriptionAndRoom()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToBeVisibleAsync();

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToBeVisibleAsync();

        var roomNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomNumberField).ToBeVisibleAsync();

        // Verify Instructions field is after Description
        var descriptionBox = await descriptionField.BoundingBoxAsync();
        var instructionsBox = await instructionsField.BoundingBoxAsync();
        var roomBox = await roomNumberField.BoundingBoxAsync();

        descriptionBox.ShouldNotBeNull();
        instructionsBox.ShouldNotBeNull();
        roomBox.ShouldNotBeNull();

        // Instructions should be below Description (higher Y position)
        instructionsBox.Y.ShouldBeGreaterThan(descriptionBox.Y);

        // Room should be below Instructions (higher Y position)
        roomBox.Y.ShouldBeGreaterThan(instructionsBox.Y);

        await TakeScreenshotAsync(1, "FieldPlacementVerified");
    }
}

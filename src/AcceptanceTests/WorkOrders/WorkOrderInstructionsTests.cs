using System.Globalization;
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
        order.Title = "from automation";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var testInstructions = new string('x', 4000);
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
        await TakeScreenshotAsync(2, "FormFilledWith4000CharInstructions");

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
        rehyratedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "from automation";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
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
        // Intentionally not filling Instructions field
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
        await TakeScreenshotAsync(2, "FormFilledWithEmptyInstructions");

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
        rehyratedOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldAddInstructionsToExistingWorkOrderAndVerifyPersistence()
    {
        await LoginAsCurrentUser();

        // Create work order without instructions
        var order = await CreateAndSaveNewWorkOrder();

        // Navigate back to work order search
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click on the work order to edit
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Add instructions
        var testInstructions = "These are new instructions added later";
        await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);

        // Assign to user
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);

        // Save changes
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify persistence by querying database
        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        if (rehyratedOrder == null)
        {
            await Task.Delay(1000);
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        }
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Instructions.ShouldBe(testInstructions);
        rehyratedOrder.Assignee.ShouldNotBeNull();
        rehyratedOrder.Assignee!.UserName.ShouldBe(CurrentUser.UserName);

        // Navigate back to the work order to verify UI shows updated instructions
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(testInstructions);
    }
}

using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using NavMenu = ClearMeasure.Bootcamp.UI.Shared.NavMenu;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test Empty Instructions";
        order.Description = "Description for test";
        order.Instructions = null;
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await TakeScreenshotAsync(2, "FormFilledWithoutInstructions");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Test Long Instructions";
        order.Description = "Description for test";
        order.Instructions = new string('X', 4000);
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        await TakeScreenshotAsync(2, "FormFilledWith4000CharInstructions");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Instructions.ShouldNotBeNull();
        rehyratedOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task ShouldSaveWorkOrderThenAddInstructionsAndAssign()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        order = await ClickWorkOrderNumberFromSearchPage(order);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var newInstructions = "Added instructions after initial save";
        await Input(nameof(WorkOrderManage.Elements.Instructions), newInstructions);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehyratedOrder.Instructions.ShouldBe(newInstructions);
        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public async Task ShouldPersistInstructionsThroughWorkflow()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = "Workflow Test";
        order.Description = "Testing instructions through workflow";
        order.Instructions = "Step 1: Turn off power\nStep 2: Replace fixture\nStep 3: Turn power back on";
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

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order = await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(order.Instructions!);

        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(order.Instructions!);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(order.Instructions!);

        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(order.Instructions!);

        var rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehyratedOrder.Instructions.ShouldBe(order.Instructions);
        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.Complete);
    }
}

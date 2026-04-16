using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldPersistWorkOrder_When_InstructionsAreMaxLength()
    {
        await LoginAsCurrentUser();

        var instructions = new string('i', 4000);
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] instructions max";
        order.Number = null;
        order.Instructions = null;
        order.Description = "desc for max instructions";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description!);
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber!);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var saved = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        saved.ShouldNotBeNull();
        saved!.Instructions!.Length.ShouldBe(4000);
        saved.Instructions.ShouldBe(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldPersistNullInstructions_When_InstructionsLeftEmptyOnCreate()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();
        rehydrated!.Instructions.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructionsAfterEdit_When_AssigningFromDraft()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        var addedInstructions = "Shut off water before replacing valve.";
        await Input(nameof(WorkOrderManage.Elements.Instructions), addedInstructions);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(addedInstructions);

        var afterAssign = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        afterAssign.ShouldNotBeNull();
        afterAssign!.Instructions.ShouldBe(addedInstructions);
    }
}

using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsAcceptanceTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var instructions = new string('i', 4000);
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] instructions max length";
        order.Number = null;
        order.Description = "Description for instructions test";
        order.Instructions = instructions;
        order.RoomNumber = "101";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title!);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description!);
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber!);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();
        rehydrated.Instructions.ShouldNotBeNull();
        rehydrated.Instructions!.Length.ShouldBe(4000);
        rehydrated.Instructions.ShouldBe(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 15; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
            if (rehydrated != null) break;
            await Task.Delay(500);
        }
        rehydrated.ShouldNotBeNull();
        rehydrated!.Instructions.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructionsAfterSaveAssignAndReturn()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order.Instructions = "Bring ladder from storage.";
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(order.Instructions!);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();
        rehydrated.Instructions.ShouldBe("Bring ladder from storage.");
    }
}

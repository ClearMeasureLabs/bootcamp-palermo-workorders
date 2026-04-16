using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Playwright;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldPersist_When_InstructionsAreMaxLength()
    {
        await LoginAsCurrentUser();

        var instructions = new string('i', 4000);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = (await woNumberLocator.InnerTextAsync()).Trim();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] instructions max");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc");
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(number));
            if (rehydrated != null) break;
            await Task.Delay(1000);
        }

        rehydrated.ShouldNotBeNull();
        rehydrated!.Instructions.ShouldBe(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldPersist_When_InstructionsAreEmpty()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = (await woNumberLocator.InnerTextAsync()).Trim();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] no instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc only");
        await Input(nameof(WorkOrderManage.Elements.Instructions), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "202");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(number));
            if (rehydrated != null) break;
            await Task.Delay(1000);
        }

        rehydrated.ShouldNotBeNull();
        rehydrated!.Instructions.ShouldBe(string.Empty);
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructions_When_AddedAfterSaveThenAssigned()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order.Instructions.ShouldBe(string.Empty);

        order = await ClickWorkOrderNumberFromSearchPage(order);

        var added = "Bring tools from shed";
        await Input(nameof(WorkOrderManage.Elements.Instructions), added);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var afterSave = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        afterSave.Instructions.ShouldBe(added);

        order = await ClickWorkOrderNumberFromSearchPage(afterSave);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);

        var afterAssign = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        afterAssign.Instructions.ShouldBe(added);
    }
}

using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
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
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var instructions = new string('i', 4000);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] instructions max");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc");
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? loaded = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            loaded = await Bus.Send(new WorkOrderByNumberQuery(number));
            if (loaded != null) break;
            await Task.Delay(1000);
        }

        loaded.ShouldNotBeNull();
        loaded!.Instructions.ShouldBe(instructions);
        loaded.Instructions!.Length.ShouldBe(4000);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] no instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc only");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "202");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? loaded = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            loaded = await Bus.Send(new WorkOrderByNumberQuery(number));
            if (loaded != null) break;
            await Task.Delay(1000);
        }

        loaded.ShouldNotBeNull();
        string.IsNullOrEmpty(loaded!.Instructions).ShouldBeTrue();
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructionsAfterEditAssignAndReload()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var instructions = "Bring extension ladder and spare bulbs.";
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);

        WorkOrder rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydrated.Instructions.ShouldBe(instructions);
    }
}

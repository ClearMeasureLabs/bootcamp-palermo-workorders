using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Microsoft.Playwright;
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

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(number));
        rehydrated.ShouldNotBeNull();
        rehydrated.Instructions.ShouldBe(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldPersistWorkOrder_When_InstructionsAreEmpty()
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
        await Input(nameof(WorkOrderManage.Elements.Instructions), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "202");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(number));
        rehydrated.ShouldNotBeNull();
        rehydrated.Instructions.ShouldBe("");
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructions_When_AddedAfterSaveThenAssigned()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(order.Number!)}\\?mode=Edit"));

        await Input(nameof(WorkOrderManage.Elements.Instructions), "Follow up after parts arrive.");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();
        rehydrated.Instructions.ShouldBe("Follow up after parts arrive.");
    }
}

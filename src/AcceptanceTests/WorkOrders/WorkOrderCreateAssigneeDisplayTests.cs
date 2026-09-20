using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

/// <summary>
/// #9754 — assignee display on create: the manage page and the search row render the
/// assignee's full name, and an unassigned work order renders "Unassigned".
/// </summary>
[NonParallelizable]
public class WorkOrderCreateAssigneeDisplayTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldShowAssigneeFullName_AfterCreateWithAssignee()
    {
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var workOrderNumber = (await woNumberLocator.InnerTextAsync()).Trim();

        await Select(nameof(WorkOrderManage.Elements.Assignee), "gwillie");
        await Input(nameof(WorkOrderManage.Elements.Title), $"mow front grass {TestTag}");
        await Input(nameof(WorkOrderManage.Elements.Description), "edge the walk");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "front lawn");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Reopen the work order and assert the assignee select holds gwillie.
        await Page.GotoAsync($"/workorder/manage/{workOrderNumber}?mode=Edit");
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(woNumberLocator).ToHaveTextAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee))).ToHaveValueAsync("gwillie");

        // The search row for this exact work order shows the assignee's full name.
        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var rowLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        await Expect(rowLink).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        var row = Page.Locator("tr", new PageLocatorOptions { Has = rowLink });
        await Expect(row.Locator("td:nth-child(3)")).ToHaveTextAsync("Groundskeeper Willie MacDougal");
    }

    [Test]
    public async Task ShouldShowUnassigned_AfterCreateWithoutAssignee()
    {
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var workOrderNumber = (await woNumberLocator.InnerTextAsync()).Trim();

        // Leave the assignee blank.
        await Input(nameof(WorkOrderManage.Elements.Title), $"fix porch light {TestTag}");
        await Input(nameof(WorkOrderManage.Elements.Description), "replace the bulb");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "porch");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The search row for this exact work order shows "Unassigned".
        var rowLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        await Expect(rowLink).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        var row = Page.Locator("tr", new PageLocatorOptions { Has = rowLink });
        await Expect(row.Locator("td:nth-child(3)")).ToHaveTextAsync("Unassigned");
    }
}

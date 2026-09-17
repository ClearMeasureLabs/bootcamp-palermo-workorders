using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderRoomSelectorTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWithRoomAndVerifyOnDetailPage()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        var orderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] room selector create");
        await Input(nameof(WorkOrderManage.Elements.Description), "testing room dropdown");

        // Select the first available room from the dropdown
        var firstRoomId = await GetFirstRoomIdAsync();
        await Select(nameof(WorkOrderManage.Elements.RoomNumber), firstRoomId);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to the saved work order
        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(new WorkOrder { Number = orderNumber });

        // Assert the room dropdown shows the selected room
        var roomField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomField).ToHaveValueAsync(firstRoomId);

        // Assert the work order has Room populated in the DB
        WorkOrder rehydrated = await Bus.Send(new WorkOrderByNumberQuery(orderNumber))
            ?? throw new InvalidOperationException();
        rehydrated.Room.ShouldNotBeNull("Room should be set on the persisted work order");
    }

    [Test, Retry(2)]
    public async Task ShouldRequireRoomSelectionOnSave()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] no room");
        await Input(nameof(WorkOrderManage.Elements.Description), "leaving room blank");

        // Leave room dropdown at blank placeholder — do not select a room
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Form must stay on manage page and show validation message
        await Expect(Page).ToHaveURLAsync(new Regex("workorder/manage"));
        await Expect(Page.GetByText("Room is required.")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
    }

    [Test, Retry(2)]
    public async Task ShouldLoadExistingWorkOrderWithNoRoomWithoutError()
    {
        // Seed a work order with RoomId = NULL directly via EF
        await using var context = TestHost.NewDbContext();
        var creator = await context.Set<Employee>()
            .FirstAsync(e => e.UserName == CurrentUser.UserName);

        var orderNumber = $"NR{TestTag[..4]}";
        var noRoomOrder = new WorkOrder
        {
            Number = orderNumber,
            Title = $"[{TestTag}] no room seeded",
            Description = "seeded without a Room FK",
            Status = WorkOrderStatus.Draft,
            Creator = creator
            // Room intentionally null
        };
        context.Add(noRoomOrder);
        await context.SaveChangesAsync();

        // Navigate to edit page — should load without error
        await LoginAsCurrentUser();
        await Page.GotoAsync($"/workorder/manage/{orderNumber}?mode=Edit");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(woNumberLocator).ToHaveTextAsync(orderNumber);

        // Room dropdown must be visible with blank placeholder selected (value = "")
        var roomField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomField).ToBeVisibleAsync();
        var roomValue = await roomField.InputValueAsync();
        roomValue.ShouldBeNullOrEmpty("Existing work order without Room should show blank dropdown");
    }
}

using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchStatusBadgeTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ClickingAssignedStatusBadge_FiltersResultsToAssignedOnly()
    {
        var creator = Faker<Employee>();
        var assignedOrder = Faker<WorkOrder>();
        assignedOrder.Creator = creator;
        assignedOrder.Status = WorkOrderStatus.Assigned;
        assignedOrder.Title = $"[{TestTag}] assigned badge";

        var draftOrder = Faker<WorkOrder>();
        draftOrder.Creator = creator;
        draftOrder.Status = WorkOrderStatus.Draft;
        draftOrder.Title = $"[{TestTag}] draft badge";

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(assignedOrder);
        context.Add(draftOrder);
        await context.SaveChangesAsync();

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var badge = Page.Locator($"[data-testid='{WorkOrderSearch.Elements.StatusBadge}{assignedOrder.Number}']");
        await Expect(badge).ToBeVisibleAsync();
        await badge.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");
        await Expect(statusSelect).ToHaveValueAsync(WorkOrderStatus.Assigned.Key);

        var statusCells = Page.Locator(".grid-data tbody tr td:nth-child(4)");
        var count = await statusCells.CountAsync();
        count.ShouldBeGreaterThanOrEqualTo(1);
        for (var i = 0; i < count; i++)
        {
            await Expect(statusCells.Nth(i)).ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
        }
    }
}

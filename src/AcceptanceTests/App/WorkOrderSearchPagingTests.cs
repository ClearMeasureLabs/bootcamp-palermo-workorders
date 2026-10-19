using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class WorkOrderSearchPagingTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldPageForwardAndBack_OnWorkOrderSearch()
    {
        await LoginAsCurrentUser();
        await EnsureEnoughWorkOrdersForPaging();

        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("#SearchButton").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var pager = Page.GetByTestId("SearchPager");
        await pager.WaitForAsync();
        await Expect(pager).ToBeVisibleAsync();

        var previous = Page.GetByTestId("SearchPagerPrevious");
        var next = Page.GetByTestId("SearchPagerNext");
        var label = Page.GetByTestId("SearchPagerLabel");

        await Expect(previous).ToBeDisabledAsync();
        await Expect(next).ToBeEnabledAsync();
        await Expect(label).ToContainTextAsync("Page 1 of");

        await next.ClickAsync();
        await Expect(label).ToContainTextAsync("Page 2 of");
        await Expect(previous).ToBeEnabledAsync();

        await previous.ClickAsync();
        await Expect(label).ToContainTextAsync("Page 1 of");
        await Expect(previous).ToBeDisabledAsync();
    }

    private async Task EnsureEnoughWorkOrdersForPaging()
    {
        await using var context = TestHost.NewDbContext();
        var existingCount = await context.Set<WorkOrder>().CountAsync();
        if (existingCount > 25)
        {
            return;
        }

        context.Attach(CurrentUser);
        var toAdd = 26 - existingCount;
        for (var i = 0; i < toAdd; i++)
        {
            var order = Faker<WorkOrder>();
            order.Creator = CurrentUser;
            order.Assignee = CurrentUser;
            order.Title = $"[{TestTag}] paging {i}";
            order.CreatedDate = DateTime.UtcNow.AddSeconds(-i);
            context.Add(order);
        }

        await context.SaveChangesAsync();
    }
}

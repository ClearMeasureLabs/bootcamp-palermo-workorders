using ClearMeasure.Bootcamp.UI.Shared.Pages;

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
        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("#SearchButton").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        if (await Page.GetByTestId("SearchPager").CountAsync() > 0)
        {
            return;
        }

        for (var i = 0; i < 26; i++)
        {
            await Page.GotoAsync("/workorder/manage?mode=New");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Input(nameof(WorkOrderManage.Elements.Title), $"Paging title {i}");
            await Input(nameof(WorkOrderManage.Elements.Description), $"Paging description {i}");
            await Input(nameof(WorkOrderManage.Elements.RoomNumber), $"{100 + i}");
            await Page.Locator("button[type='submit']").First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}

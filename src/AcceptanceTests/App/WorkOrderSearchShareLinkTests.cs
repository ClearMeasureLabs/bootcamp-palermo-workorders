using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class WorkOrderSearchShareLinkTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldOpenShareLink_WithStatusFilter_SelectedAndApplied()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}").SelectOptionAsync("InProgress");
        await Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var shareBox = Page.GetByTestId(nameof(WorkOrderSearch.Elements.ShareThisSearch));
        await shareBox.WaitForAsync();
        var shareLink = shareBox.Locator("a").First;
        var href = await shareLink.GetAttributeAsync("href");
        href.ShouldNotBeNullOrEmpty();
        href.ShouldContain("Status=InProgress");

        await Page.GotoAsync(href);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");
        await Expect(statusSelect).ToHaveValueAsync("InProgress");
    }
}

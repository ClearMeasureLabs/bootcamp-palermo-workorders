using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class StatusDashboardSearchNavigationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldNavigateToSearchWithDraftFilter_WhenDraftCardClicked()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var draftKey = WorkOrderStatus.Draft.Key;
        var draftCard = Page.GetByTestId(nameof(IndexPage.Elements.StatusCard) + draftKey);
        await Expect(draftCard).ToBeVisibleAsync();
        await draftCard.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($@"/workorder/search\?status={Regex.Escape(draftKey)}"));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var draftRows = Page.Locator("table tbody tr", new() { HasText = WorkOrderStatus.Draft.FriendlyName });
        var otherRows = Page.Locator("table tbody tr")
            .Filter(new() { HasNotText = WorkOrderStatus.Draft.FriendlyName });
        await Expect(draftRows.First).ToBeVisibleAsync();
        await Expect(otherRows).ToHaveCountAsync(0);
    }
}

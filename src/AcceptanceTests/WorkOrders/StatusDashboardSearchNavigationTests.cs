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
        await draftCard.WaitForAsync();
        await draftCard.ClickAsync();

        await Page.WaitForURLAsync(new Regex($@"/workorder/search\?status={Regex.Escape(draftKey)}"));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("select").First).ToHaveValueAsync(draftKey);

        var rows = Page.Locator("table tbody tr");
        var rowCount = await rows.CountAsync();
        for (var i = 0; i < rowCount; i++)
        {
            await Expect(rows.Nth(i)).ToContainTextAsync(WorkOrderStatus.Draft.FriendlyName);
        }
    }
}

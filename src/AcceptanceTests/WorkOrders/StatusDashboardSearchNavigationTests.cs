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

        var draftCard = Page.GetByTestId(nameof(IndexPage.Elements.StatusCard) + WorkOrderStatus.Draft.Key);
        await draftCard.WaitForAsync();
        await draftCard.ClickAsync();

        await Page.WaitForURLAsync($"**/workorder/search?status={WorkOrderStatus.Draft.Key}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator($"select >> option[value='{WorkOrderStatus.Draft.Key}']")).ToHaveJSPropertyAsync("selected", true);

        var rows = Page.Locator("table tbody tr");
        await rows.First.WaitForAsync();
        var rowCount = await rows.CountAsync();
        for (var i = 0; i < rowCount; i++)
        {
            await Expect(rows.Nth(i)).ToContainTextAsync(WorkOrderStatus.Draft.FriendlyName);
        }
    }
}

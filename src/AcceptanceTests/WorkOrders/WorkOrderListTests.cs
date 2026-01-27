using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderListTests : AcceptanceTestBase
{
    [Test]
    public async Task WorkOrderListButtons_AreGreen()
    {
        // Login as current user
        await LoginAsCurrentUser();

        // Navigate to work orders list
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        // Verify "Search" button is green
        var searchButton = Page.GetByTestId(nameof(WorkOrderSearch.Elements.SearchButton));
        await Expect(searchButton).ToBeVisibleAsync();

        var backgroundColor = await searchButton.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
        backgroundColor.ShouldContain("34, 197, 94"); // RGB for green (#22c55e)
        await TakeScreenshotAsync(2);
    }
}

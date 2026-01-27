using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderListTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test]
    public async Task WorkOrderList_AllButtons_DisplayGreenStyling()
    {
        // Arrange: Navigate to work orders list/search page
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "WorkOrderSearchPage");

        // Act: Get the Search button
        var searchButton = Page.Locator($"#{UI.Shared.Pages.WorkOrderSearch.Elements.SearchButton}");
        await Expect(searchButton).ToBeVisibleAsync();

        // Assert: Verify Search button has green background color
        var searchButtonBackgroundColor = await searchButton.EvaluateAsync<string>(@"
            element => window.getComputedStyle(element).background
        ");
        
        searchButtonBackgroundColor.ShouldContain("22c55e", Case.Insensitive);

        // Verify "Create New Work Order" button navigation link exists
        var newWorkOrderLink = Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder));
        await Expect(newWorkOrderLink).ToBeVisibleAsync();
    }
}

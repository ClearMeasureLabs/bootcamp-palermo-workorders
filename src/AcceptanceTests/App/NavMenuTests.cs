namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavMenuTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Counter_NavLink_ShouldNotBeVisible()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The Counter nav link used data-testid="Counter"; verify it is absent from the DOM.
        var counterLink = Page.GetByTestId("Counter");
        await Expect(counterLink).Not.ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task FetchData_NavLink_ShouldNotBeVisible()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The Fetch data nav link had no testid; locate by href and verify it is absent.
        var fetchDataLink = Page.Locator("a.nav-link[href='fetchdata']");
        await Expect(fetchDataLink).Not.ToBeVisibleAsync();
    }
}

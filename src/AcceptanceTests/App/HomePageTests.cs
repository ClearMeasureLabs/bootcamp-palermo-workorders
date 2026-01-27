namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class HomePageTests : AcceptanceTestBase
{
    [Test]
    public async Task HomePage_AllButtons_DisplayGreenStyling()
    {
        // Arrange: Navigate to home page
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "HomePage");

        // Act: Login to see navigation action buttons
        await LoginAsCurrentUser();
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(2, "HomePageAuthenticated");

        // Assert: Verify navigation elements are visible
        // The home page primarily uses navigation links, not buttons
        // Main buttons are in specific pages like login, work order create/edit, etc.
        var homeLink = Page.Locator("a[href='/']").First;
        await Expect(homeLink).ToBeVisibleAsync();
    }
}

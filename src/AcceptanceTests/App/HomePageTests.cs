namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class HomePageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_DisplayWelcomeHeading_WithInclusiveText()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var welcomeHeading = Page.Locator(".welcome-message h2");
        await Expect(welcomeHeading).ToBeVisibleAsync();
        await Expect(welcomeHeading).ToContainTextAsync("Welcome, Fellow Church Staff and Volunteers!");
    }

    [Test, Retry(2)]
    public async Task Should_DisplayAllHomePageContent_WithUnchangedLayout()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        // Verify church title exists
        var churchTitle = Page.Locator(".church-title");
        await Expect(churchTitle).ToBeVisibleAsync();
        await Expect(churchTitle).ToContainTextAsync("First Church of Springfield");

        // Verify welcome message
        var welcomeHeading = Page.Locator(".welcome-message h2");
        await Expect(welcomeHeading).ToBeVisibleAsync();
        await Expect(welcomeHeading).ToContainTextAsync("Welcome, Fellow Church Staff and Volunteers!");

        // Verify subtitle
        var subtitle = Page.Locator(".church-subtitle");
        await Expect(subtitle).ToBeVisibleAsync();
        await Expect(subtitle).ToContainTextAsync("Reverend Timothy Lovejoy Jr., Pastor");

        // Verify info cards exist
        var workOrdersCard = Page.Locator(".info-card", new PageLocatorOptions { HasTextString = "Work Orders" });
        await Expect(workOrdersCard).ToBeVisibleAsync();

        var volunteersCard = Page.Locator(".info-card", new PageLocatorOptions { HasTextString = "Volunteers" });
        await Expect(volunteersCard).ToBeVisibleAsync();

        var scheduleCard = Page.Locator(".info-card", new PageLocatorOptions { HasTextString = "Schedule" });
        await Expect(scheduleCard).ToBeVisibleAsync();

        // Verify footer exists
        var footer = Page.Locator(".church-footer");
        await Expect(footer).ToBeVisibleAsync();
        await Expect(footer).ToContainTextAsync("Blessed are those who maintain the house of the Lord");
    }
}

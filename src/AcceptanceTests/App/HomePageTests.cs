namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class HomePageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_DisplayWelcomeHeading_WithInclusiveText()
    {
        // Arrange
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        // Act
        var welcomeHeading = Page.Locator("h2").Filter(new LocatorFilterOptions { HasText = "Welcome, Fellow Church Staff and Volunteers!" });

        // Assert
        await Expect(welcomeHeading).ToBeVisibleAsync();
        await Expect(welcomeHeading).ToContainTextAsync("Welcome, Fellow Church Staff and Volunteers!");
    }

    [Test, Retry(2)]
    public async Task Should_PreserveAllOtherHomePageContent()
    {
        // Arrange
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        // Act & Assert - Check that other key page elements remain unchanged
        var churchTitle = Page.Locator(".church-title");
        await Expect(churchTitle).ToContainTextAsync("First Church of Springfield");

        var churchSubtitle = Page.Locator(".church-subtitle");
        await Expect(churchSubtitle).ToContainTextAsync("Reverend Timothy Lovejoy Jr., Pastor");

        var infoCards = Page.Locator(".info-card");
        await Expect(infoCards).ToHaveCountAsync(3);

        var workOrderCard = infoCards.Filter(new LocatorFilterOptions { HasText = "Work Orders" });
        await Expect(workOrderCard).ToBeVisibleAsync();

        var volunteersCard = infoCards.Filter(new LocatorFilterOptions { HasText = "Volunteers" });
        await Expect(volunteersCard).ToBeVisibleAsync();

        var scheduleCard = infoCards.Filter(new LocatorFilterOptions { HasText = "Schedule" });
        await Expect(scheduleCard).ToBeVisibleAsync();

        var quote = Page.Locator(".lovejoy-quote");
        await Expect(quote).ToContainTextAsync("Remember, we're here to help our community");

        var footer = Page.Locator(".church-footer");
        await Expect(footer).ToContainTextAsync("Blessed are those who maintain the house of the Lord");
    }
}

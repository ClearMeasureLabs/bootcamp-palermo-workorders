namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    protected override bool LoadDataOnSetup { get; set; } = false;

    [Test]
    public async Task Should_DisplayChurchTitle_WithDarkGreyColor()
    {
        // Arrange - Already on landing page from SetUpAsync
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        // Act
        var titleElement = Page.Locator(".church-title");
        await titleElement.WaitForAsync();

        // Assert
        var titleColor = await titleElement.EvaluateAsync<string>("element => window.getComputedStyle(element).color");
        
        // Convert #a9a9a9 to rgb(169, 169, 169)
        titleColor.ShouldBe("rgb(169, 169, 169)");
    }

    [Test]
    public async Task Should_DisplayHammerEmoji_ForWorkOrders()
    {
        // Arrange - Already on landing page from SetUpAsync
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        // Act
        var workOrdersHeading = Page.Locator("h3:has-text('Work Orders')");
        await workOrdersHeading.WaitForAsync();
        var headingText = await workOrdersHeading.TextContentAsync();

        // Assert
        headingText.ShouldNotBeNull();
        headingText.ShouldContain("🔨");
        headingText.ShouldNotContain("🏗️");
    }
}

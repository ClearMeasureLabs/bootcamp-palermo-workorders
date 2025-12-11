namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    protected override bool LoadDataOnSetup { get; set; } = false;

    [Test]
    public async Task Should_DisplayChurchTitle_WithWhiteColor()
    {
        // Arrange - Already on landing page from SetUpAsync
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        // Act
        var titleElement = Page.Locator(".church-title");
        await titleElement.WaitForAsync();

        // Assert
        var titleColor = await titleElement.EvaluateAsync<string>("element => window.getComputedStyle(element).color");
        
        // Convert #ffffff to rgb(255, 255, 255)
        titleColor.ShouldBe("rgb(255, 255, 255)");
    }
}

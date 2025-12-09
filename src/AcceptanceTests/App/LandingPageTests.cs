namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    protected override bool LoadDataOnSetup { get; set; } = false;

    [Test]
    public async Task Should_DisplayLatinQuoteOnLandingPage()
    {
        // Arrange
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var pageContent = await Page.ContentAsync();

        // Assert
        pageContent.ShouldContain("Hic sumus ad nostram communitatem adiuvandam... et ne scamna diruantur.");
        pageContent.ShouldContain("- Springfield Church Manual");
    }
}

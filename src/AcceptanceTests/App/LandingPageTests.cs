namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    [Test]
    public async Task Should_DisplayNewInspirationalQuote_OnLandingPage()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var pageContent = await Page.ContentAsync();
        pageContent.ShouldContain("Hic sumus ad nostram communitatem adiuvandam... et ne scamna diruantur.");
        pageContent.ShouldContain("Springfield Church Manual");
    }
}

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    protected override bool LoadDataOnSetup => false;

    [Test]
    public async Task Should_DisplayInspirationalQuote_OnLandingPage()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());
        
        var pageContent = await Page.ContentAsync();
        pageContent.ShouldContain("Hic sumus ad nostram communitatem adiuvandam... et ne scamna diruantur.");
        pageContent.ShouldContain("Springfield Church Manual");
    }
}

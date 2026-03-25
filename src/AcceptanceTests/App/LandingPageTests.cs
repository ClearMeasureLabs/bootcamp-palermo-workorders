using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_DisplayChurchTitle_WithDarkGreyColor()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var titleElement = Page.Locator(".church-title");
        await titleElement.WaitForAsync();

        var titleColor = await titleElement.EvaluateAsync<string>("element => window.getComputedStyle(element).color");
        
        titleColor.ShouldBe("rgb(169, 169, 169)");
    }

    [Test, Retry(2)]
    public async Task Should_DisplayGreetingBanner_WithExpectedText()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var banner = Page.GetByTestId(nameof(IndexPage.Elements.GreetingBanner));
        await banner.WaitForAsync();

        await Expect(banner).ToBeVisibleAsync();
        await Expect(banner).ToContainTextAsync("Welcome to the AI Software Factory!");
    }
}

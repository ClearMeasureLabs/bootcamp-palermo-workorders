using System.Globalization;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class CopyrightFooterTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowCopyrightFooter_OnLandingPage_WhenAnonymous()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var footer = Page.GetByTestId(nameof(MainLayout.Elements.CopyrightFooter));
        await footer.WaitForAsync();
        await Expect(footer).ToBeVisibleAsync();

        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        await Expect(footer).ToContainTextAsync(yearText);
        await Expect(footer).ToContainTextAsync("ClearMeasure Labs");

        var link = footer.Locator("a[href*='clearmeasure.com']").First;
        await Expect(link).ToBeVisibleAsync();
        var href = (await link.GetAttributeAsync("href"))!.ToLowerInvariant();
        href.ShouldStartWith("http");
        href.ShouldContain("clearmeasure.com");
    }

    [Test, Retry(2)]
    public async Task ShouldShowCopyrightFooter_OnAuthenticatedRoute_AfterLogin()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var footer = Page.GetByTestId(nameof(MainLayout.Elements.CopyrightFooter));
        await Expect(footer).ToBeVisibleAsync();
        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        await Expect(footer).ToContainTextAsync(yearText);
        await Expect(footer).ToContainTextAsync("ClearMeasure Labs");
    }

    [Test, Retry(2)]
    public async Task ShouldShowCopyrightFooter_OnNotFoundRoute()
    {
        await Page.GotoAsync("/this-route-does-not-exist-1842");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var footer = Page.GetByTestId(nameof(MainLayout.Elements.CopyrightFooter));
        await footer.WaitForAsync();
        await footer.ScrollIntoViewIfNeededAsync();
        await Expect(footer).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Sorry, there's nothing at this address.");
    }
}

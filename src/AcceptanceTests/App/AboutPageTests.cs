using Microsoft.Playwright;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class AboutPageTests : AcceptanceTestBase
{
    [Test]
    public async Task Should_DisplayAboutPageWithBuiltWithDotNetBadge()
    {
        await Page.GotoAsync(ServerUrl + "/about");
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var heading = Page.Locator("h1");
        await heading.WaitForAsync();
        await Expect(heading).ToHaveTextAsync("About");
        
        var badge = Page.Locator(".built-with-badge");
        await badge.WaitForAsync();
        await Expect(badge).ToHaveTextAsync("Built with .NET");
    }
    
    [Test]
    public async Task Should_NavigateToAboutPageFromMenu()
    {
        await Page.GotoAsync(ServerUrl);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var aboutLink = Page.Locator("a[href='about']");
        await aboutLink.WaitForAsync();
        await aboutLink.ClickAsync();
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var heading = Page.Locator("h1");
        await heading.WaitForAsync();
        await Expect(heading).ToHaveTextAsync("About");
        
        var badge = Page.Locator(".built-with-badge");
        await Expect(badge).ToHaveTextAsync("Built with .NET");
    }
}

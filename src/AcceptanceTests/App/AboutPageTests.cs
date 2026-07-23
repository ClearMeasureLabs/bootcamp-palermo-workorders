using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[Collection("Playwright")]
public class AboutPageTests : AcceptanceTestBase
{
    [Fact]
    public async Task ShouldDisplayAboutPageWithBuiltWithDotNetBadge()
    {
        await Page.GotoAsync(ServerUrl + "/about");
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var heading = Page.Locator("h1");
        await heading.WaitForAsync();
        (await heading.TextContentAsync()).Should().Be("About");
        
        var badge = Page.Locator(".built-with-badge");
        await badge.WaitForAsync();
        (await badge.TextContentAsync()).Should().Be("Built with .NET");
    }
    
    [Fact]
    public async Task ShouldNavigateToAboutPageFromMenu()
    {
        await Page.GotoAsync(ServerUrl);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var aboutLink = Page.Locator("a[href='about']");
        await aboutLink.WaitForAsync();
        await aboutLink.ClickAsync();
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var heading = Page.Locator("h1");
        await heading.WaitForAsync();
        (await heading.TextContentAsync()).Should().Be("About");
        
        var badge = Page.Locator(".built-with-badge");
        (await badge.TextContentAsync()).Should().Be("Built with .NET");
    }
}

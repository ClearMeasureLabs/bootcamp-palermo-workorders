using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavRailToggleTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowNavRailToggleInHeaderBeforeTitle_WhenAuthenticated()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync();

        var title = Page.Locator(".header-title h3").First;
        await Expect(title).ToBeVisibleAsync();

        var toggleBox = await toggle.BoundingBoxAsync();
        var titleBox = await title.BoundingBoxAsync();
        toggleBox.ShouldNotBeNull();
        titleBox.ShouldNotBeNull();
        toggleBox!.X.ShouldBeLessThan(titleBox!.X);

        var rail = Page.Locator("#app-navigation-rail");
        await Expect(rail).ToBeVisibleAsync();
        var railClass = await rail.GetAttributeAsync("class");
        railClass.ShouldNotBeNull();
        railClass.ShouldNotContain("rail-hidden");

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Test, Retry(2)]
    public async Task ShouldCollapseNavRailAndExpandMainContent_WhenToggleClickedOnDesktop()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        var rail = Page.Locator("#app-navigation-rail");
        var railClass = await rail.GetAttributeAsync("class");
        railClass.ShouldNotBeNull();
        railClass.ShouldContain("rail-hidden");

        var appContainer = Page.Locator(".modern-app").First;
        var appClass = await appContainer.GetAttributeAsync("class");
        appClass.ShouldNotBeNull();
        appClass.ShouldContain("rail-collapsed");

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToHaveAttributeAsync("title", new Regex("Show", RegexOptions.IgnoreCase));
    }

    [Test, Retry(2)]
    public async Task ShouldRestoreNavRail_WhenToggleClickedTwiceOnDesktop()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        var rail = Page.Locator("#app-navigation-rail");
        var railClass = await rail.GetAttributeAsync("class");
        railClass.ShouldNotBeNull();
        railClass.ShouldNotContain("rail-hidden");

        var appContainer = Page.Locator(".modern-app").First;
        var appClass = await appContainer.GetAttributeAsync("class");
        appClass.ShouldNotBeNull();
        appClass.ShouldNotContain("rail-collapsed");

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(toggle).ToHaveAttributeAsync("title", new Regex("Hide", RegexOptions.IgnoreCase));
    }

    [Test, Retry(2)]
    public async Task ShouldOpenNavRailOverlay_WhenToggleClickedOnNarrowViewport()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var rail = Page.Locator("#app-navigation-rail");
        var initialClass = await rail.GetAttributeAsync("class");
        initialClass.ShouldNotBeNull();
        initialClass.ShouldNotContain("open");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        var openClass = await rail.GetAttributeAsync("class");
        openClass.ShouldNotBeNull();
        openClass.ShouldContain("open");
    }

    [Test, Retry(2)]
    public async Task ShouldNotBreakPrimaryNavigation_WhenRailVisible()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        await Expect(Page).ToHaveURLAsync(new Regex("/search", RegexOptions.IgnoreCase));
    }
}

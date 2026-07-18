using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavRailToggleTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldHideAndShowNavigationRail_OnWideViewport_AfterLogin()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var urlBefore = Page.Url;
        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync();

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        var rail = Page.Locator("#app-navigation-rail");
        (await rail.GetAttributeAsync("class"))!.ShouldContain("rail-hidden");
        (await Page.Locator(".modern-app").GetAttributeAsync("class"))!.ShouldContain("rail-collapsed");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        (await rail.GetAttributeAsync("class"))!.ShouldNotContain("rail-hidden");
        (await Page.Locator(".modern-app").GetAttributeAsync("class"))!.ShouldNotContain("rail-collapsed");
        Page.Url.ShouldBe(urlBefore);
    }

    [Test, Retry(2)]
    public async Task ShouldKeepAriaExpandedInSyncWithNavVisibility()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("true");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("false");
        (await toggle.GetAttributeAsync("title"))!.ShouldContain("Show");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("true");
        (await toggle.GetAttributeAsync("title"))!.ShouldContain("Hide");
    }

    [Test, Retry(2)]
    public async Task ShouldExpandContentArea_WhenNavHidden_OnWorkRequestPage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workrequest/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var urlBefore = Page.Url;
        var searchButton = Page.Locator($"#{nameof(WorkRequestSearch.Elements.SearchButton)}");
        await Expect(searchButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        (await Page.Locator(".modern-app").GetAttributeAsync("class"))!.ShouldContain("rail-collapsed");
        await Expect(searchButton).ToBeVisibleAsync();
        Page.Url.ShouldBe(urlBefore);
    }

    [Test, Retry(2)]
    public async Task ShouldOpenAndCloseMobileOverlay_WhenNarrowViewport()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workrequest/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        await Page.SetViewportSizeAsync(375, 667);
        await Task.Delay(GetInputDelayMs() * 2);

        var rail = Page.Locator("#app-navigation-rail");
        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        (await rail.GetAttributeAsync("class"))!.ShouldContain("open");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        (await rail.GetAttributeAsync("class"))!.ShouldNotContain("open");
        await Expect(toggle).ToBeFocusedAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowNavRailToggle_OnAnonymousLandingPage()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync();

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        (await Page.Locator("#app-navigation-rail").GetAttributeAsync("class"))!.ShouldContain("rail-hidden");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        (await Page.Locator("#app-navigation-rail").GetAttributeAsync("class"))!.ShouldNotContain("rail-hidden");
    }
}

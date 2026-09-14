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

        var urlBefore = Page.Url;
        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync();

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        var rail = Page.Locator("#app-navigation-rail");
        (await rail.GetAttributeAsync("class"))!.ShouldContain("rail-hidden");
        (await Page.Locator(".modern-app").GetAttributeAsync("class"))!.ShouldContain("rail-collapsed");

        await Click(nameof(MainLayout.Elements.NavRailToggle));

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

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("true");

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("false");
        (await toggle.GetAttributeAsync("title"))!.ShouldContain("Show");

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("true");
        (await toggle.GetAttributeAsync("title"))!.ShouldContain("Hide");
    }

    [Test, Retry(2)]
    public async Task ShouldExpandContentArea_WhenNavHidden_OnWorkOrderPage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var urlBefore = Page.Url;
        var searchButton = Page.Locator($"#{nameof(WorkOrderSearch.Elements.SearchButton)}");
        await Expect(searchButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        (await Page.Locator(".modern-app").GetAttributeAsync("class"))!.ShouldContain("rail-collapsed");
        await Expect(searchButton).ToBeVisibleAsync();
        Page.Url.ShouldBe(urlBefore);
    }

    [Test, Retry(2)]
    public async Task ShouldOpenAndCloseMobileOverlay_WhenNarrowViewport()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        await Page.SetViewportSizeAsync(375, 667);

        var rail = Page.Locator("#app-navigation-rail");
        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        (await rail.GetAttributeAsync("class"))!.ShouldContain("open");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        (await rail.GetAttributeAsync("class"))!.ShouldNotContain("open");
        await Expect(toggle).ToBeFocusedAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowBackdrop_WhenNavOpenOnMobile()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Hide nav while wide so _navVisible=false; sidebar gets rail-hidden class
        await Click(nameof(MainLayout.Elements.NavRailToggle));
        var rail = Page.Locator("#app-navigation-rail");
        await Expect(rail).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("rail-hidden"),
            new LocatorAssertionsToHaveClassOptions { Timeout = 5_000 });

        await Page.SetViewportSizeAsync(375, 667);
        // Wait for Blazor to process OnViewportChanged — narrow mode drops rail-hidden
        await Expect(rail).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("rail-hidden"),
            new LocatorAssertionsToHaveClassOptions { Timeout = 5_000 });

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        var backdrop = Page.Locator(".nav-backdrop");
        await Expect(backdrop).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldDismissNavByTappingBackdrop_OnMobile()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Hide nav while wide so _navVisible=false; sidebar gets rail-hidden class
        await Click(nameof(MainLayout.Elements.NavRailToggle));
        var rail = Page.Locator("#app-navigation-rail");
        await Expect(rail).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("rail-hidden"),
            new LocatorAssertionsToHaveClassOptions { Timeout = 5_000 });

        await Page.SetViewportSizeAsync(375, 667);
        // Wait for Blazor to process OnViewportChanged — narrow mode drops rail-hidden
        await Expect(rail).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("rail-hidden"),
            new LocatorAssertionsToHaveClassOptions { Timeout = 5_000 });

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        var backdrop = Page.Locator(".nav-backdrop");
        await Expect(backdrop).ToBeVisibleAsync();

        // Click the backdrop at a position that is NOT covered by the sidebar (width ≤ 280px).
        // The viewport is 375 px wide; clicking at x=340 lands on the backdrop outside the sidebar.
        await backdrop.ClickAsync(new LocatorClickOptions { Position = new Position { X = 340, Y = 333 } });

        await Expect(rail).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("open"),
            new LocatorAssertionsToHaveClassOptions { Timeout = 5_000 });
        await Expect(backdrop).ToBeHiddenAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowNavRailToggle_OnAnonymousLandingPage()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // First interactive element after a cold anonymous WASM boot: allow generous warmup.
        // browserContext.SetDefaultTimeout does not apply to web-first assertions (they default to
        // 5s), so this must be explicit - matching the pattern used elsewhere in this suite.
        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        (await Page.Locator("#app-navigation-rail").GetAttributeAsync("class"))!.ShouldContain("rail-hidden");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        (await Page.Locator("#app-navigation-rail").GetAttributeAsync("class"))!.ShouldNotContain("rail-hidden");
    }
}

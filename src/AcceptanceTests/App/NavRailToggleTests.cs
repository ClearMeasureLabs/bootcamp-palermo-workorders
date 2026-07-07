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
        await Expect(rail).ToHaveClassAsync(new Regex("rail-hidden"));
        await Expect(Page.Locator(".modern-app")).ToHaveClassAsync(new Regex("rail-collapsed"));

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        await Expect(rail).Not.ToHaveClassAsync(new Regex("rail-hidden"));
        await Expect(Page.Locator(".modern-app")).Not.ToHaveClassAsync(new Regex("rail-collapsed"));
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
    public async Task ShouldExpandContentArea_WhenNavHidden_OnWorkOrderPage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var urlBefore = Page.Url;
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.SearchButton))).ToBeVisibleAsync();

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());

        await Expect(Page.Locator(".modern-app")).ToHaveClassAsync(new Regex("rail-collapsed"));
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.SearchButton))).ToBeVisibleAsync();
        Page.Url.ShouldBe(urlBefore);
    }

    [Test, Retry(2)]
    public async Task ShouldOpenAndCloseMobileOverlay_WhenNarrowViewport()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var rail = Page.Locator("#app-navigation-rail");
        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        await Expect(rail).ToHaveClassAsync(new Regex("open"));

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        await Expect(rail).Not.ToHaveClassAsync(new Regex("open"));
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
        await Expect(Page.Locator("#app-navigation-rail")).ToHaveClassAsync(new Regex("rail-hidden"));

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Task.Delay(GetInputDelayMs());
        await Expect(Page.Locator("#app-navigation-rail")).Not.ToHaveClassAsync(new Regex("rail-hidden"));
    }
}

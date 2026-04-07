using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class MainLayoutNavRailTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_HideAndRestoreNavRail_OnWideViewport()
    {
        await Page.SetViewportSizeAsync(1200, 800);
        await LoginAsCurrentUser();

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

        var app = Page.Locator(".modern-app").First;
        var rail = Page.Locator("#app-navigation-rail");

        await Expect(app).Not.ToContainClassAsync("rail-collapsed");
        await Expect(rail).Not.ToContainClassAsync("rail-hidden");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToHaveAttributeAsync("title", new Regex("Show", RegexOptions.IgnoreCase));
        await Expect(app).ToContainClassAsync("rail-collapsed");
        await Expect(rail).ToContainClassAsync("rail-hidden");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(toggle).ToHaveAttributeAsync("title", new Regex("Hide", RegexOptions.IgnoreCase));
        await Expect(app).Not.ToContainClassAsync("rail-collapsed");
        await Expect(rail).Not.ToContainClassAsync("rail-hidden");
    }

    [Test, Retry(2)]
    public async Task Should_OpenAndCloseNavOverlay_OnNarrowViewport()
    {
        await LoginAsCurrentUser();
        await Page.SetViewportSizeAsync(390, 844);
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        var rail = Page.Locator("#app-navigation-rail");

        await Expect(toggle).ToBeVisibleAsync();
        await Expect(rail).Not.ToContainClassAsync("open");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(rail).ToContainClassAsync("open");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(rail).Not.ToContainClassAsync("open");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Test, Retry(2)]
    public async Task Should_NavigateFromMenu_WhenRailVisibleAfterToggle()
    {
        await Page.SetViewportSizeAsync(1200, 800);
        await LoginAsCurrentUser();

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Click(nameof(MainLayout.Elements.NavRailToggle));

        await Click(nameof(NavMenu.Elements.Counter));
        await Page.WaitForURLAsync("**/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var valueLocator = Page.GetByTestId(nameof(Counter.Elements.CounterValue));
        await Expect(valueLocator).ToBeVisibleAsync();
        await Expect(valueLocator).ToHaveTextAsync("0");
    }
}

using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavRailToggleTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCollapseAndExpandNavRail_OnWideViewport()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#app-navigation-rail")).ToHaveClassAsync(new Regex("rail-hidden"));
        await Expect(Page.Locator(".modern-app").First).ToHaveClassAsync(new Regex("rail-collapsed"));

        await Click(nameof(MainLayout.Elements.NavRailToggle));

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#app-navigation-rail")).Not.ToHaveClassAsync(new Regex("rail-hidden"));
        await Expect(Page.Locator(".modern-app").First).Not.ToHaveClassAsync(new Regex("rail-collapsed"));
    }
}

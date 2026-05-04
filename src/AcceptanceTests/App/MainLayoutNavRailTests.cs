using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class MainLayoutNavRailTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldToggleNavRail_AndUpdateAria_OnLandingPage()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-controls", "app-navigation-rail");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");

        var app = Page.Locator(".modern-app").First;
        await Expect(app).ToHaveClassAsync(new Regex("rail-collapsed"));

        await Click(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(app).Not.ToHaveClassAsync(new Regex("rail-collapsed"));
    }
}

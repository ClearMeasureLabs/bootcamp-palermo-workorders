using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavRailToggleTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task NavRail_ShouldCollapseMainColumn_OnWideViewportWhenToggledAfterLogin()
    {
        await LoginAsCurrentUser();

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await toggle.WaitForAsync();

        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("true");
        (await toggle.GetAttributeAsync("title"))!.ShouldContain("Hide");

        await toggle.EvaluateAsync("el => el.click()");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("false");
        (await toggle.GetAttributeAsync("title"))!.ShouldContain("Show");
        var collapsedAppClass = (await Page.Locator(".modern-app").GetAttributeAsync("class")) ?? "";
        collapsedAppClass.ShouldContain("rail-collapsed");

        var hiddenRailClass = (await Page.Locator("#app-navigation-rail").GetAttributeAsync("class")) ?? "";
        hiddenRailClass.ShouldContain("rail-hidden");

        await toggle.EvaluateAsync("el => el.click()");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("true");
        var expandedAppClass = (await Page.Locator(".modern-app").GetAttributeAsync("class")) ?? "";
        expandedAppClass.ShouldNotContain("rail-collapsed");
    }

    [Test, Retry(2)]
    public async Task NavRail_ShouldOpenOverlay_OnNarrowViewportWhenToggledAfterLogin()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await LoginAsCurrentUser();

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await toggle.WaitForAsync();

        var rail = Page.Locator("#app-navigation-rail");

        await Expect(rail).Not.ToHaveClassAsync(new Regex(@"\bopen\b"));
        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("false");

        await toggle.EvaluateAsync("el => el.click()");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(rail).ToHaveClassAsync(new Regex(@"\bopen\b"));
        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("true");

        await toggle.EvaluateAsync("el => el.click()");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(rail).Not.ToHaveClassAsync(new Regex(@"\bopen\b"));
        (await toggle.GetAttributeAsync("aria-expanded")).ShouldBe("false");
    }
}

using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavRailToggleAttributeParityTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldHaveMatchingTitleAndAriaLabelOnNavRailToggle()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var toggle = Page.GetByTestId(nameof(MainLayout.Elements.NavRailToggle));
        await Expect(toggle).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        var title = await toggle.GetAttributeAsync("title");
        var ariaLabel = await toggle.GetAttributeAsync("aria-label");
        title.ShouldNotBeNull();
        ariaLabel.ShouldNotBeNull();
        title.ShouldBe(ariaLabel);
    }
}

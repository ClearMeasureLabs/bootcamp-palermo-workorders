using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class HealthCheckLinkAttributeParityTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldHaveMatchingTitleAndAriaLabelOnHealthCheckLink()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var link = Page.GetByTestId(nameof(HealthCheckLink.Elements.HealthCheckLink));
        await Expect(link).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        var title = await link.GetAttributeAsync("title");
        var ariaLabel = await link.GetAttributeAsync("aria-label");
        title.ShouldBe("Health Check");
        ariaLabel.ShouldBe("Health Check");
        title.ShouldBe(ariaLabel);
    }
}

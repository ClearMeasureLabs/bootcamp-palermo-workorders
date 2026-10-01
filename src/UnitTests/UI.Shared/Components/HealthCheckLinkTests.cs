using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class HealthCheckLinkTests
{
    [Test]
    public async Task Should_RenderDescriptiveTooltip_OnOutermostAnchor()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<HealthCheckLink>();

        var link = component.Find($"[data-testid='{nameof(HealthCheckLink.Elements.HealthCheckLink)}']");
        link.GetAttribute("title").ShouldBe("View application health status");
        link.GetAttribute("href").ShouldBe("/_clienthealthcheck");
    }
}

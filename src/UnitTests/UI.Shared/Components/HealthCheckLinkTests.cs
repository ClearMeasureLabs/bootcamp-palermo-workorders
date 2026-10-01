using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class HealthCheckLinkTests
{
    [Test]
    public async Task Should_RenderDescriptiveTooltip_OnOutermostAnchor()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<HealthCheckLink>();

        var link = component.Find($"[data-testid='{nameof(HealthCheckLink.Elements.HealthCheckLink)}']");
        link.GetAttribute("title").ShouldBe("View application health status");
        link.GetAttribute("href").ShouldBe("/_clienthealthcheck");
    }
}

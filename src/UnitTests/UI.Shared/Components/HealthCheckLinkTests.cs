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

    [Test]
    public async Task Should_RenderLabel_WhenShowLabelIsTrue()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<HealthCheckLink>(parameters => parameters
            .Add(c => c.ShowLabel, true));

        var link = component.Find($"[data-testid='{nameof(HealthCheckLink.Elements.HealthCheckLink)}']");
        link.TextContent.ShouldContain("Health Check");
        link.GetAttribute("title").ShouldBe("View application health status");
        link.QuerySelector("i.bi-gear-fill").ShouldNotBeNull();
    }

    [Test]
    public async Task Should_OmitLabel_WhenShowLabelIsFalse()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<HealthCheckLink>(parameters => parameters
            .Add(c => c.ShowLabel, false));

        var link = component.Find($"[data-testid='{nameof(HealthCheckLink.Elements.HealthCheckLink)}']");
        link.TextContent.ShouldNotContain("Health Check");
        link.GetAttribute("title").ShouldBe("View application health status");
        link.GetAttribute("href").ShouldBe("/_clienthealthcheck");
        link.QuerySelector("i.bi-gear-fill").ShouldNotBeNull();
    }
}

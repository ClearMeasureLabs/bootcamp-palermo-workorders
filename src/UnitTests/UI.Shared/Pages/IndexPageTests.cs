using Bunit;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class IndexPageTests
{
    [Test]
    public async Task ShouldDisplayGreetingBanner()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.AddAuthorization();

        var component = ctx.Render<IndexPage>();

        var banner = component.Find($"[data-testid='{nameof(IndexPage.Elements.GreetingBanner)}']");
        banner.ShouldNotBeNull();
        banner.TextContent.ShouldContain("Welcome to the AI Software Factory! Have a blessed day.");

        var emoji = component.Find($"[data-testid='{nameof(IndexPage.Elements.GreetingBannerEmoji)}']");
        emoji.GetAttribute("aria-hidden").ShouldBe("true");
        emoji.TextContent.ShouldContain("⛪");
    }

    [Test]
    public async Task ShouldDisplayHomeTagline()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.AddAuthorization();

        var component = ctx.Render<IndexPage>();

        var tagline = component.Find($"[data-testid='{nameof(IndexPage.Elements.HomeTagline)}']");
        tagline.TextContent.ShouldBe("Track and manage work orders in one place.");
    }

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnHomeHeading()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.AddAuthorization();

        var component = ctx.Render<IndexPage>();

        var heading = component.Find($"[data-testid='{nameof(IndexPage.Elements.HomeHeadingTooltip)}']");
        heading.GetAttribute("title").ShouldBe("Work order home");
    }
}

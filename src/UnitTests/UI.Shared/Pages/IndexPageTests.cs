using Bunit;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class IndexPageTests
{
    [Test]
    public void ShouldDisplaySampleDataNote()
    {
        using var ctx = new TestContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.RenderComponent<IndexPage>();

        var note = component.Find($"[data-testid='{nameof(IndexPage.Elements.SampleDataNote)}']");
        note.ShouldNotBeNull();
        note.TextContent.Trim().ShouldBe("Sample data loaded");
    }

    [Test]
    public void ShouldDisplayGreetingBanner()
    {
        using var ctx = new TestContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.RenderComponent<IndexPage>();

        var banner = component.Find($"[data-testid='{nameof(IndexPage.Elements.GreetingBanner)}']");
        banner.ShouldNotBeNull();
        banner.TextContent.ShouldContain("Welcome to the AI Software Factory!");

        var emoji = component.Find($"[data-testid='{nameof(IndexPage.Elements.GreetingBannerEmoji)}']");
        emoji.GetAttribute("aria-hidden").ShouldBe("true");
        emoji.TextContent.ShouldContain("⛪");
    }
}

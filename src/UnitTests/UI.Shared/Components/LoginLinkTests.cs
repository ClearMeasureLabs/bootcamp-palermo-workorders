using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LoginLinkTests
{
    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnLoginLink()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<LoginLink>();

        var link = component.Find($"[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        link.GetAttribute("title").ShouldBe("Sign in to manage work orders");
    }
}

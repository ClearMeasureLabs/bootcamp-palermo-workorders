using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class NavMenuTests
{
    [Test]
    public void NavMenu_ShouldRenderWeatherLinkWithFetchDataHref()
    {
        using var ctx = CreateContext();
        var component = ctx.Render<NavMenu>();

        var link = component.Find($"[data-testid='{nameof(NavMenu.Elements.Weather)}']");
        link.GetAttribute("href").ShouldBe("fetchdata");
        link.TextContent.ShouldContain("Weather");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(null);
    }
}

using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class NavMenuTests
{
    [Test]
    public void ShouldRenderNavBarTogglerWithTitleAndAriaLabel()
    {
        using var ctx = new TestContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());

        var component = ctx.RenderComponent<NavMenu>();

        var toggler = component.Find("button.navbar-toggler");
        toggler.GetAttribute("title").ShouldBe("Navigation menu");
        toggler.GetAttribute("aria-label").ShouldBe("Navigation menu");
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(null);
    }
}

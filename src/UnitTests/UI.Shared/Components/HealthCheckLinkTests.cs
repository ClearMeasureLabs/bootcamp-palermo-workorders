using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class HealthCheckLinkTests
{
    [Test]
    public void ShouldRenderHealthCheckLinkWithTitleAndAriaLabel()
    {
        using var ctx = new TestContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<HealthCheckLink>();

        var link = component.Find($"[data-testid='{nameof(HealthCheckLink.Elements.HealthCheckLink)}']");
        link.GetAttribute("title").ShouldBe("Health Check");
        link.GetAttribute("aria-label").ShouldBe("Health Check");
    }
}

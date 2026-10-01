using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class CounterPageTests
{
    [Test]
    public async Task Should_DisplaySubtitle_WithVolunteerServiceText()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();

        component.Find($"[data-testid='{nameof(Counter.Elements.CounterSubtitle)}']")
            .TextContent
            .ShouldBe("Track church maintenance activities and volunteer service");
    }

    [Test]
    public async Task Should_DisplayUsageHint_WithExactClickInstruction()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();

        component.Find($"[data-testid='{nameof(Counter.Elements.CounterHint)}']")
            .TextContent
            .ShouldBe("Click the button to increase the count.");
    }
}

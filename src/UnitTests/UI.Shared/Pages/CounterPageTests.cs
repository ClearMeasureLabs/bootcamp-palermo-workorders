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

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnIncrementButton()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();

        var button = component.Find($"[data-testid='{nameof(Counter.Elements.IncrementButton)}']");
        button.GetAttribute("title").ShouldBe("Increase the count by one");
    }

    [Test]
    public async Task Should_HideGoalReached_WhenCountIsBelowTen()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();
        var increment = component.Find($"[data-testid='{nameof(Counter.Elements.IncrementButton)}']");
        for (var i = 0; i < 9; i++)
        {
            await increment.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        }

        Should.Throw<Bunit.ElementNotFoundException>(() =>
            component.Find($"[data-testid='{nameof(Counter.Elements.GoalReached)}']"));
    }

    [Test]
    public async Task Should_ShowGoalReached_WhenCountIsTen()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();
        var increment = component.Find($"[data-testid='{nameof(Counter.Elements.IncrementButton)}']");
        for (var i = 0; i < 10; i++)
        {
            await increment.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        }

        component.Find($"[data-testid='{nameof(Counter.Elements.GoalReached)}']")
            .TextContent
            .ShouldBe("Goal reached!");
    }
}

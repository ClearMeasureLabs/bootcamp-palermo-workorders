using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Bunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LastWorkOrderTests
{
    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnLastWorkOrderLink()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<LastWorkOrder>();

        var sampleWorkOrder = new WorkOrder { Number = "WO-001" };
        component.Instance.Handle(new WorkOrderChangedEvent(new StateCommandResult(sampleWorkOrder)));
        component.Render();

        var link = component.Find($"[data-testid='{nameof(LastWorkOrder.Elements.LastWorkOrderTooltip)}']");
        link.GetAttribute("title").ShouldBe("Most recently viewed work order");
    }

    [Test]
    public async Task ShouldNotRenderTooltipHookWhenNoLastWorkOrder()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<LastWorkOrder>();

        component.FindAll($"[data-testid='{nameof(LastWorkOrder.Elements.LastWorkOrderTooltip)}']")
            .ShouldBeEmpty();
    }
}

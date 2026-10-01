using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Bunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderChatTests
{
    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnWorkOrderChatPanel()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<WorkOrderChat>();

        var workOrder = new WorkOrder
        {
            Number = "WO-001",
            Assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
        };
        await component.InvokeAsync(() => component.Instance.Handle(new WorkOrderSelectedEvent(workOrder)));
        component.Render();

        var panel = component.Find($"[data-testid='{nameof(WorkOrderChat.Elements.WorkOrderChatTooltip)}']");
        panel.GetAttribute("title").ShouldBe("Chat about this work order");
    }

    [Test]
    public async Task ShouldNotRenderTooltipHookWhenChatPanelHidden()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<WorkOrderChat>();

        var workOrder = new WorkOrder
        {
            Number = "WO-002",
            Assignee = null
        };
        await component.InvokeAsync(() => component.Instance.Handle(new WorkOrderSelectedEvent(workOrder)));
        component.Render();

        component.FindAll($"[data-testid='{nameof(WorkOrderChat.Elements.WorkOrderChatTooltip)}']")
            .ShouldBeEmpty();
    }
}

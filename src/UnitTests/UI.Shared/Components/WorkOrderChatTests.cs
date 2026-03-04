using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderChatTests
{
    [Test]
    public void OnKeyDownAsync_EnterKey_CallsSendMessage()
    {
        using var ctx = CreateContext();
        var stubBus = new StubBusForWorkOrderChat();
        ctx.Services.AddSingleton<IBus>(stubBus);

        var component = ctx.RenderComponent<WorkOrderChat>();
        SimulateWorkOrderSelected(ctx, component);

        component.Find($"[data-testid='{WorkOrderChat.Elements.ChatInput}']").Change("test prompt");
        component.Find($"[data-testid='{WorkOrderChat.Elements.ChatInput}']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        component.WaitForAssertion(() =>
        {
            stubBus.SendWasCalled.ShouldBeTrue();
        });
    }

    [Test]
    public void OnKeyDownAsync_NonEnterKey_DoesNotCallSendMessage()
    {
        using var ctx = CreateContext();
        var stubBus = new StubBusForWorkOrderChat();
        ctx.Services.AddSingleton<IBus>(stubBus);

        var component = ctx.RenderComponent<WorkOrderChat>();
        SimulateWorkOrderSelected(ctx, component);

        component.Find($"[data-testid='{WorkOrderChat.Elements.ChatInput}']").Change("test prompt");
        component.Find($"[data-testid='{WorkOrderChat.Elements.ChatInput}']").KeyDown(new KeyboardEventArgs { Key = "a" });

        stubBus.SendWasCalled.ShouldBeFalse();
    }

    [Test]
    public void OnKeyDownAsync_EnterKeyWithEmptyPrompt_DoesNotCallSendMessage()
    {
        using var ctx = CreateContext();
        var stubBus = new StubBusForWorkOrderChat();
        ctx.Services.AddSingleton<IBus>(stubBus);

        var component = ctx.RenderComponent<WorkOrderChat>();
        SimulateWorkOrderSelected(ctx, component);

        component.Find($"[data-testid='{WorkOrderChat.Elements.ChatInput}']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        stubBus.SendWasCalled.ShouldBeFalse();
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }

    private static void SimulateWorkOrderSelected(TestContext ctx, IRenderedComponent<WorkOrderChat> component)
    {
        var assignee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-001",
            Title = "Test Work Order",
            Status = WorkOrderStatus.Assigned,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com"),
            Assignee = assignee
        };

        component.InvokeAsync(() => component.Instance.Handle(new WorkOrderSelectedEvent(workOrder)));
    }

    private sealed class StubBusForWorkOrderChat() : Bus(null!)
    {
        public bool SendWasCalled { get; private set; }

        public override Task Publish(INotification notification)
        {
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderChatQuery)
            {
                SendWasCalled = true;
                var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Test response")]);
                return Task.FromResult((TResponse)(object)response);
            }

            throw new NotImplementedException();
        }
    }
}

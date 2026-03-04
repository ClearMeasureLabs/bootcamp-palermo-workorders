using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class ApplicationChatTests
{
    [Test]
    public void ShouldRenderChatShellAndInputElements()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<ApplicationChat>();

        component.Find($"[data-testid='{ApplicationChat.Elements.ChatShell}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatContainer}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatInputContainer}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ShouldNotBeNull();
    }

    [Test]
    public void ShouldRenderChatHistoryViewportAfterSendingMessage()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<ApplicationChat>();
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").Change("first prompt");
        component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").Click();

        component.WaitForAssertion(() =>
        {
            component.Find($"[data-testid='{ApplicationChat.Elements.ChatHistory}']").ShouldNotBeNull();
            component.Find($"[data-testid='{ApplicationChat.Elements.ChatHistoryViewport}']").ShouldNotBeNull();
            component.FindAll(".chat-message").Count.ShouldBeGreaterThanOrEqualTo(2);
        });
    }

    [Test]
    public void ShouldKeepPromptInputAvailableAfterManyMessages()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<ApplicationChat>();

        for (var i = 0; i < 12; i++)
        {
            var prompt = $"Prompt {i}";
            component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").Change(prompt);
            component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").Click();
            component.WaitForAssertion(() =>
            {
                component.Markup.ShouldContain(prompt);
            });
        }

        component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ShouldNotBeNull();
        component.FindAll(".chat-message").Count.ShouldBeGreaterThanOrEqualTo(24);
    }

    [Test]
    public void ShouldSendMessageWhenEnterKeyPressed()
    {
        using var ctx = CreateContext();
        var component = ctx.RenderComponent<ApplicationChat>();

        var chatInput = component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']");
        chatInput.Change("test prompt via enter");
        chatInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        component.WaitForAssertion(() =>
        {
            component.Find($"[data-testid='{ApplicationChat.Elements.ChatHistory}']").ShouldNotBeNull();
            component.FindAll(".chat-message").Count.ShouldBeGreaterThanOrEqualTo(1);
            component.Markup.ShouldContain("test prompt via enter");
        });
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("scrollToBottom", _ => true);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new ApplicationChatStubBus());

        var provider = new CustomAuthenticationStateProvider();
        provider.Login("hsimpson");
        ctx.Services.AddSingleton(provider);

        return ctx;
    }

    private sealed class ApplicationChatStubBus() : Bus(null!)
    {
        public override Task Publish(INotification notification)
        {
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ApplicationChatQuery)
            {
                throw new InvalidOperationException("Simulated AI service failure");
            }

            throw new NotImplementedException();
        }
    }
}

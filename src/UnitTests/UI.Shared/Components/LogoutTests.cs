using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LogoutTests
{
    [Test]
    public void ShouldDisplayWelcomeMessageWithUsername()
    {
        using var ctx = new TestContext();

        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.RenderComponent<Logout>();

        var welcomeSpan = component.Find("span");
        welcomeSpan.TextContent.ShouldContain("Welcome");
        welcomeSpan.TextContent.ShouldContain("hsimpson");
    }

    [Test]
    public void ShouldDisplayLogoutLink()
    {
        using var ctx = new TestContext();

        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.RenderComponent<Logout>();

        var logoutLink = component.Find("a");
        logoutLink.ShouldNotBeNull();
        logoutLink.TextContent.ShouldBe("Logout");
        logoutLink.GetAttribute("href").ShouldBe("#");
        logoutLink.GetAttribute("class")!.ShouldContain("ms-3");
    }

    [Test]
    public void ShouldNotifyEventBusWithUserLoggedOutEventOnClick()
    {
        using var ctx = new TestContext();

        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");
        var spyEventBus = new SpyUiBus();

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(spyEventBus);
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.RenderComponent<Logout>();
        var logoutLink = component.Find("a");

        logoutLink.Click();

        spyEventBus.NotifyWasCalled.ShouldBeTrue();
        spyEventBus.LastNotifiedEvent.ShouldBeOfType<UserLoggedOutEvent>();
    }

    [Test]
    public void ShouldNavigateToLoginPageOnClick()
    {
        using var ctx = new TestContext();

        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.RenderComponent<Logout>();
        var logoutLink = component.Find("a");

        logoutLink.Click();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.ShouldEndWith("/login");
    }

    [Test]
    public void ShouldPerformAllLogoutActionsInCorrectOrder()
    {
        using var ctx = new TestContext();

        var spyEventBus = new SpyUiBus();

        ctx.Services.AddSingleton<CustomAuthenticationStateProvider>();
        ctx.Services.AddSingleton<IUiBus>(spyEventBus);
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.RenderComponent<Logout>();
        var logoutLink = component.Find("a");

        logoutLink.Click();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        // Verify all actions occurred
        spyEventBus.NotifyWasCalled.ShouldBeTrue();
        spyEventBus.LastNotifiedEvent.ShouldBeOfType<UserLoggedOutEvent>();
        navigationManager.Uri.ShouldEndWith("/login");
    }
}

public class SpyUiBus : IUiBus
{
    public bool NotifyWasCalled { get; private set; }
    public object? LastNotifiedEvent { get; private set; }

    public void Notify(object eventObject)
    {
        NotifyWasCalled = true;
        LastNotifiedEvent = eventObject;
    }

    public void Register(IListener listener)
    {
    }

    public void UnRegister(IListener listener)
    {
    }

    public IListener<T>[] GetListeners<T>() where T : IUiBusEvent
    {
        return Array.Empty<IListener<T>>();
    }

    public void Notify<T>(T eventObject) where T : IUiBusEvent
    {
        NotifyWasCalled = true;
        LastNotifiedEvent = eventObject;
    }

    public void UnRegisterAll()
    {
    }
}
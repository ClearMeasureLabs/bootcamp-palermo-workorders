using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
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
    private static TestContext CreateContext(CustomAuthenticationStateProvider authProvider)
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(
            new Employee("hsimpson", "Homer", "Simpson", "homer@test.com")));
        return ctx;
    }

    [Test]
    public void ShouldDisplayWelcomeMessageWithUsername()
    {
        using var ctx = new TestContext();

        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(
            new Employee("hsimpson", "Homer", "Simpson", "homer@test.com")));

        var component = ctx.RenderComponent<Logout>();

        var welcomeSpan = component.Find($"[data-testid='{nameof(Logout.Elements.WelcomeText)}']");
        welcomeSpan.TextContent.ShouldContain("Welcome");
        welcomeSpan.TextContent.ShouldContain("hsimpson");
    }

    [Test]
    public void ShouldDisplayLogoutLink()
    {
        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");
        using var ctx = CreateContext(authProvider);

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
        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");
        using var ctx = CreateContext(authProvider);
        var spyEventBus = new SpyUiBus();
        ctx.Services.AddSingleton<IUiBus>(spyEventBus);

        var component = ctx.RenderComponent<Logout>();
        var logoutLink = component.Find("a");

        logoutLink.Click();

        spyEventBus.NotifyWasCalled.ShouldBeTrue();
        spyEventBus.LastNotifiedEvent.ShouldBeOfType<UserLoggedOutEvent>();
    }

    [Test]
    public void ShouldNavigateToLoginPageOnClick()
    {
        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");
        using var ctx = CreateContext(authProvider);

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
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));

        var component = ctx.RenderComponent<Logout>();
        var logoutLink = component.Find("a");

        logoutLink.Click();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        spyEventBus.NotifyWasCalled.ShouldBeTrue();
        spyEventBus.LastNotifiedEvent.ShouldBeOfType<UserLoggedOutEvent>();
        navigationManager.Uri.ShouldEndWith("/login");
    }

    private sealed class StubUserSession(Employee? employee) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult(employee);
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
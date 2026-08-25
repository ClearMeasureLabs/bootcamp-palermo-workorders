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
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LogoutTests
{
    [Test]
    public async Task ShouldDisplayWelcomeMessageWithUsername()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();

        var welcomeSpan = component.Find("span");
        welcomeSpan.TextContent.ShouldContain("Welcome");
        welcomeSpan.TextContent.ShouldContain("hsimpson");
    }

    [Test]
    public async Task ShouldDisplayLogoutButton()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();

        var logoutButton = component.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']");
        logoutButton.TagName.ShouldBe("BUTTON");
        logoutButton.GetAttribute("type").ShouldBe("button");
        logoutButton.TextContent.ShouldBe("Logout");
        logoutButton.GetAttribute("href").ShouldBeNull();
        logoutButton.GetAttribute("class")!.ShouldContain("ms-3");
    }

    [Test]
    public async Task ShouldNotifyEventBusWithUserLoggedOutEventOnClick()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var spyEventBus = new SpyUiBus();

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(spyEventBus);
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();
        var logoutButton = component.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']");

        await logoutButton.ClickAsync(new());

        spyEventBus.NotifyWasCalled.ShouldBeTrue();
        spyEventBus.LastNotifiedEvent.ShouldBeOfType<UserLoggedOutEvent>();
    }

    [Test]
    public async Task ShouldNavigateToLoginPageOnClick()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();
        var logoutButton = component.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']");

        await logoutButton.ClickAsync(new());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.ShouldEndWith("/login");
    }

    [Test]
    public async Task ShouldPerformAllLogoutActionsInCorrectOrder()
    {
        await using var ctx = new BunitContext();

        var spyEventBus = new SpyUiBus();
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(spyEventBus);
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();
        var logoutButton = component.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']");

        await logoutButton.ClickAsync(new());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        spyEventBus.NotifyWasCalled.ShouldBeTrue();
        spyEventBus.LastNotifiedEvent.ShouldBeOfType<UserLoggedOutEvent>();
        navigationManager.Uri.ShouldEndWith("/login");
        authProvider.IsAuthenticated().ShouldBeFalse();
    }

    [Test]
    public async Task ShouldNotNavigate_WhenLogoutFailsClosed()
    {
        await using var ctx = new BunitContext();

        var store = new StubUserSessionStore { KeepUsernameOnClear = true };
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("tlovejoy");
        var spyEventBus = new SpyUiBus();

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(spyEventBus);
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();
        var logoutButton = component.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']");
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uriBefore = navigationManager.Uri;

        await Should.ThrowAsync<InvalidOperationException>(() => logoutButton.ClickAsync(new()));

        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("tlovejoy");
        spyEventBus.NotifyWasCalled.ShouldBeFalse();
        navigationManager.Uri.ShouldBe(uriBefore);
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

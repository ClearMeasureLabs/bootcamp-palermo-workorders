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
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();

        var welcomeSpan = component.Find("span");
        welcomeSpan.TextContent.ShouldContain("Welcome");
        welcomeSpan.TextContent.ShouldContain("hsimpson");
    }

    [Test]
    public async Task ShouldDisplayFullNameWithMiddleName_WhenCurrentUserHasMiddleName()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com") { MiddleName = "Jay" };

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(employee));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();

        var welcomeSpan = component.Find($"[data-testid='{nameof(Logout.Elements.WelcomeText)}']");
        NormalizeWhitespace(welcomeSpan.TextContent).ShouldBe("Welcome Homer Jay Simpson!");
    }

    [Test]
    public async Task ShouldDisplayUsername_WhenCurrentUserHasNoMiddleName()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(employee));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();

        var welcomeSpan = component.Find($"[data-testid='{nameof(Logout.Elements.WelcomeText)}']");
        NormalizeWhitespace(welcomeSpan.TextContent).ShouldBe("Welcome hsimpson!");
    }

    [Test]
    public async Task ShouldDisplayFullNameWithMiddleName_WhenUserLoggedInEventArrives()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var userSession = new StubUserSession(null);

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(userSession);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();
        userSession.CurrentUser = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com") { MiddleName = "Jay" };
        component.Instance.Handle(new UserLoggedInEvent("hsimpson"));

        await component.WaitForAssertionAsync(() =>
            NormalizeWhitespace(component.Find($"[data-testid='{nameof(Logout.Elements.WelcomeText)}']").TextContent)
                .ShouldBe("Welcome Homer Jay Simpson!"));
    }

    [Test]
    public async Task ShouldDisplayUsername_WhenUserLookupFails()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubFailingUserSession());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();

        var welcomeSpan = component.Find($"[data-testid='{nameof(Logout.Elements.WelcomeText)}']");
        NormalizeWhitespace(welcomeSpan.TextContent).ShouldBe("Welcome hsimpson!");
    }

    [Test]
    public async Task ShouldDisplayLogoutButton()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
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
    public async Task ShouldShowSignOutTooltipOnLogoutButton()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.Render<Logout>();

        var logoutButton = component.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']");
        logoutButton.GetAttribute("title").ShouldBe("Sign out of the application");
    }

    [Test]
    public async Task ShouldNotifyEventBusWithUserLoggedOutEventOnClick()
    {
        await using var ctx = new BunitContext();

        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var spyEventBus = new SpyUiBus();

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
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
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
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
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
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

    private static string NormalizeWhitespace(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

    [Test]
    public async Task ShouldNotNavigate_WhenLogoutFailsClosed()
    {
        await using var ctx = new BunitContext();

        var store = new StubUserSessionStore { KeepUsernameOnClear = true };
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("tlovejoy");
        var spyEventBus = new SpyUiBus();

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
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

public class StubUserSession(Employee? currentUser) : IUserSession
{
    public Employee? CurrentUser { get; set; } = currentUser;

    public Task<Employee?> GetCurrentUserAsync()
    {
        return Task.FromResult(CurrentUser);
    }
}

public class StubFailingUserSession : IUserSession
{
    public Task<Employee?> GetCurrentUserAsync()
    {
        throw new InvalidOperationException("lookup failed");
    }
}

public class SpyUiBus : IUiBus
{
    public bool NotifyWasCalled { get; private set; }
    public object? LastNotifiedEvent { get; private set; }

    // ReSharper disable once UnusedMember.Global -- required by IUiBus interface; generic Notify<T> is called at runtime
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

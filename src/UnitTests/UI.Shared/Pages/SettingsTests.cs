using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class SettingsTests
{
    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderAppearanceSectionAndDarkModeSwitch()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        component.Markup.ShouldContain("Appearance");
        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']");
        sw.ShouldNotBeNull();
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderDisplayPreferencesSubtitle()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        var subtitle = component.Find($"[data-testid='{nameof(Settings.Elements.SettingsSubtitle)}']");
        subtitle.TextContent.ShouldContain("browser");
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldToggleDarkModeViaSwitch()
    {
        await using var ctx = CreateContext();
        var theme = ctx.Services.GetRequiredService<ThemePreferenceService>();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        await component.InvokeAsync(() => Task.CompletedTask);
        theme.IsDarkMode.ShouldBeFalse();

        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']");
        await sw.ChangeAsync(new() { Value = true });

        await component.WaitForAssertionAsync(() => theme.IsDarkMode.ShouldBeTrue());
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldShowCurrentPreferredLanguage()
    {
        var bus = new SettingsStubBus
        {
            Employee = new Employee("hsimpson", "Homer", "Simpson", "h@test.com")
            {
                PreferredLanguage = "es-ES"
            }
        };
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        await component.WaitForAssertionAsync(() =>
        {
            var select = component.Find($"[data-testid='{nameof(Settings.Elements.PreferredLanguageSelect)}']");
            select.QuerySelector("option[value='es-ES']")!.HasAttribute("selected").ShouldBeTrue();
        });
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldSavePreferredLanguageThroughBus()
    {
        var bus = new SettingsStubBus();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        var select = component.Find($"[data-testid='{nameof(Settings.Elements.PreferredLanguageSelect)}']");
        await select.ChangeAsync(new() { Value = "fr-FR" });

        await component.WaitForAssertionAsync(() =>
        {
            bus.LastCommand.ShouldNotBeNull();
            bus.LastCommand!.Username.ShouldBe("hsimpson");
            bus.LastCommand.PreferredLanguage.ShouldBe("fr-FR");
        });
    }

    private static BunitContext CreateContext(IBus? bus = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.SetupModule("./_content/ClearMeasure.Bootcamp.UI.Shared/js/theme.js");
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(bus ?? new SettingsStubBus());

        var bunitAuth = ctx.AddAuthorization();
        bunitAuth.SetAuthorized("hsimpson");
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        customAuth.Login("hsimpson").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);
        ctx.Services.AddSingleton(customAuth);

        return ctx;
    }

    private sealed class SettingsStubBus : IBus
    {
        public Employee Employee { get; init; } = new("hsimpson", "Homer", "Simpson", "h@test.com")
        {
            PreferredLanguage = "en-US"
        };

        public UpdatePreferredLanguageCommand? LastCommand { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery)
            {
                return Task.FromResult((TResponse)(object)Employee);
            }

            if (request is UpdatePreferredLanguageCommand command)
            {
                LastCommand = command;
                Employee.PreferredLanguage = command.PreferredLanguage;
                return Task.FromResult((TResponse)(object)Employee);
            }

            throw new NotImplementedException();
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}

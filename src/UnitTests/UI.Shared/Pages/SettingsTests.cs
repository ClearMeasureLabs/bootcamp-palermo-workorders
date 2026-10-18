using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class SettingsTests
{
    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderAppearanceSectionAndDarkModeSwitch()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        module.Setup<bool>("getCompactTables").SetResult(false);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        component.Find("h2").TextContent.ShouldContain("Appearance");
        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']");
        sw.GetAttribute("role").ShouldBe("switch");
        component.Markup.ShouldContain("Dark mode");
        component.Markup.ShouldContain("whole app");
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderDisplayPreferencesSubtitle()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        module.Setup<bool>("getCompactTables").SetResult(false);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        var subtitle = component.Find($"[data-testid='{nameof(Settings.Elements.SettingsSubtitle)}']");
        subtitle.TextContent.ShouldBe("Manage display preferences for this browser.");
    }

    [Test]
    public async Task Settings_ToggleDarkMode_ShouldInvokeSetThemeInteropAndUpdateServiceState()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        module.Setup<bool>("getCompactTables").SetResult(false);
        module.SetupVoid("setTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());
        var theme = ctx.Services.GetRequiredService<ThemePreferenceService>();

        await component.InvokeAsync(() => Task.CompletedTask);
        theme.IsDarkMode.ShouldBeFalse();

        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']");
        await sw.ChangeAsync(new() { Value = true });

        await component.WaitForAssertionAsync(() => theme.IsDarkMode.ShouldBeTrue());
        module.VerifyInvoke("setTheme");
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderCompactTablesSwitch()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        module.Setup<bool>("getCompactTables").SetResult(false);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.CompactTablesSwitch)}']");
        sw.GetAttribute("role").ShouldBe("switch");
        sw.HasAttribute("checked").ShouldBeFalse();
    }

    [Test]
    public async Task Settings_ToggleCompactTables_ShouldPersistViaInteropAndUpdateServiceState()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        module.Setup<bool>("getCompactTables").SetResult(false);
        module.SetupVoid("setCompactTables", _ => true).SetVoidResult();
        var theme = ctx.Services.GetRequiredService<ThemePreferenceService>();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());
        await component.InvokeAsync(() => Task.CompletedTask);
        theme.IsCompactTables.ShouldBeFalse();

        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.CompactTablesSwitch)}']");
        await sw.ChangeAsync(new() { Value = true });

        await component.WaitForAssertionAsync(() => theme.IsCompactTables.ShouldBeTrue());
        module.VerifyInvoke("setCompactTables");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var bunitAuth = ctx.AddAuthorization();
        bunitAuth.SetAuthorized("hsimpson");
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        customAuth.Login("hsimpson").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);
        ctx.Services.AddSingleton(customAuth);

        return ctx;
    }
}

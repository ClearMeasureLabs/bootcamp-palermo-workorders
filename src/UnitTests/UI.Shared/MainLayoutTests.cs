using System.Globalization;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class MainLayoutTests
{
    [Test]
    public void ShouldRenderNavRailToggleWithExpandedStateByDefault()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        toggle.GetAttribute("aria-expanded").ShouldBe("true");
        toggle.GetAttribute("aria-controls").ShouldBe("app-navigation-rail");
        toggle.GetAttribute("title")!.ShouldContain("Hide");
        toggle.GetAttribute("aria-label")!.ShouldContain("Hide");
        layout.Find("#app-navigation-rail").ClassList.ShouldContain("modern-sidebar");
        layout.Find(".modern-app").ClassList.ShouldNotContain("rail-collapsed");
    }

    [Test]
    public void ShouldToggleNavRailCollapseAndUpdateAriaOnWideLayout()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        component.WaitForAssertion(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        component.InvokeAsync(() => layout.Instance.OnViewportChanged(false)).GetAwaiter().GetResult();

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        toggle.Click();

        toggle.GetAttribute("aria-expanded").ShouldBe("false");
        toggle.GetAttribute("title")!.ShouldContain("Show");
        layout.Find(".modern-app").ClassList.ShouldContain("rail-collapsed");
        layout.Find("#app-navigation-rail").ClassList.ShouldContain("rail-hidden");
    }

    [Test]
    public void ShouldUseOverlayOpenClassOnNarrowViewportWhenNavVisible()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        component.WaitForAssertion(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        component.InvokeAsync(() => layout.Instance.OnViewportChanged(true)).GetAwaiter().GetResult();

        var rail = layout.Find("#app-navigation-rail");
        rail.ClassList.ShouldNotContain("open");

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        toggle.Click();

        rail.ClassList.ShouldContain("open");
        toggle.GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Test]
    public void ShouldUseDocumentedNavRailBreakpointMediaQuery()
    {
        MainLayout.NavRailBreakpointMediaQuery.ShouldBe("(max-width: 768px)");
    }

    [Test]
    public void MainLayout_AfterFirstRender_ShouldCallThemeInitialize_WhenImplemented()
    {
        using var ctx = CreateContext();
        var themeModule = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        themeModule.Setup<string>("getTheme").SetResult("light");
        themeModule.SetupVoid("syncDomFromTheme", _ => true);

        ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        themeModule.VerifyInvoke("getTheme");
    }

    [Test]
    public void ShouldRenderLoginLink_InHeader_WhenUserIsNotAuthenticated()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var loginAnchor = layout.Find($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        loginAnchor.GetAttribute("href").ShouldBe("/login");
        loginAnchor.ClassList.ShouldContain("login-prompt-link");
    }

    [Test]
    public void ShouldNotRenderLoginLink_WhenUserIsAuthenticated()
    {
        using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        layout.FindAll($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']").Count.ShouldBe(0);
        layout.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ShouldNotBeNull();
    }

    [Test]
    public void ShouldPreserveLoginLinkInteraction_Unchanged()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var loginAnchor = layout.Find($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        loginAnchor.GetAttribute("href").ShouldBe("/login");
    }

    [Test]
    public void ShouldRenderCopyrightFooter_WithCurrentYear_OrganizationAndLink_WhenNotAuthenticated()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.TagName.ShouldBe("FOOTER");
        layout.FindAll("#app-navigation-rail footer").Count.ShouldBe(0);

        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        footer.TextContent.ShouldContain(yearText);
        footer.TextContent.ShouldContain("ClearMeasure Labs");

        var link = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link");
        link.GetAttribute("href")!.TrimEnd('/').ShouldBe("https://clearmeasure.com");
        link.TextContent.Trim().ShouldBe("ClearMeasure Labs");
    }

    [Test]
    public void ShouldRenderCopyrightFooter_WhenAuthenticated()
    {
        using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        footer.TextContent.ShouldContain(yearText);
        footer.TextContent.ShouldContain("ClearMeasure Labs");
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link").GetAttribute("href")!.TrimEnd('/').ShouldBe("https://clearmeasure.com");
    }

    [Test]
    public void ShouldRenderCompanyLink_WithAccessibleAttributes_WhenExternalLinkUsesNewTab()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var link = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link");
        link.GetAttribute("target").ShouldBe("_blank");
        var rel = link.GetAttribute("rel");
        rel.ShouldNotBeNull();
        rel.ShouldContain("noopener");
        rel.ShouldContain("noreferrer");
        link.TextContent.Trim().ShouldNotContain("://");
    }

    [Test]
    public async Task ShouldInvokeFocusOnNavRailToggleWhenClosingOverlayOnNarrowViewport()
    {
        using var ctx = CreateContext();

        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        component.WaitForAssertion(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(true));

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        toggle.Click();
        toggle.Click();

        ctx.JSInterop.VerifyFocusAsyncInvoke();
    }

    private static TestContext CreateContext(string? authenticateAsUser = null)
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bunitAuth = ctx.AddTestAuthorization();
        if (authenticateAsUser != null)
        {
            bunitAuth.SetAuthorized(authenticateAsUser);
        }

        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IJSRuntime>(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        var customAuth = new CustomAuthenticationStateProvider();
        if (authenticateAsUser != null)
        {
            customAuth.Login(authenticateAsUser);
        }

        ctx.Services.AddSingleton(customAuth);
        return ctx;
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(null);
    }
}

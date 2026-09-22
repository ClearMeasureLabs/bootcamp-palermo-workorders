using System.Globalization;
using System.Net;
using System.Text.Json;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class MainLayoutTests
{
    [Test]
    public async Task ShouldRenderNavRailToggleWithExpandedStateByDefault()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
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
    public async Task ShouldToggleNavRailCollapseAndUpdateAriaOnWideLayout()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(false));

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        toggle.GetAttribute("aria-expanded").ShouldBe("false");
        toggle.GetAttribute("title")!.ShouldContain("Show");
        layout.Find(".modern-app").ClassList.ShouldContain("rail-collapsed");
        layout.Find("#app-navigation-rail").ClassList.ShouldContain("rail-hidden");

        await toggle.ClickAsync(new());

        toggle.GetAttribute("aria-expanded").ShouldBe("true");
        toggle.GetAttribute("title")!.ShouldContain("Hide");
        layout.Find(".modern-app").ClassList.ShouldNotContain("rail-collapsed");
        layout.Find("#app-navigation-rail").ClassList.ShouldNotContain("rail-hidden");
    }

    [Test]
    public async Task ShouldRenderCorrectIconForNavVisibility()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(false));

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        toggle.InnerHtml.ShouldContain("bi-chevron-double-left");

        await toggle.ClickAsync(new());

        toggle.InnerHtml.ShouldContain("bi-list");
    }

    [Test]
    public async Task ShouldUseOverlayOpenClassOnNarrowViewportWhenNavVisible()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(true));

        var rail = layout.Find("#app-navigation-rail");
        rail.ClassList.ShouldNotContain("open");

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        rail.ClassList.ShouldContain("open");
        toggle.GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Test]
    public void ShouldUseDocumentedNavRailBreakpointMediaQuery()
    {
        MainLayout.NavRailBreakpointMediaQuery.ShouldBe("(max-width: 768px)");
    }

    [Test]
    public async Task MainLayout_AfterFirstRender_ShouldCallThemeInitialize_WhenImplemented()
    {
        await using var ctx = CreateContext();
        var themeModule = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        themeModule.Setup<string>("getTheme").SetResult("light");
        themeModule.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();

        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        themeModule.VerifyInvoke("getTheme");
    }

    [Test]
    public async Task ShouldRenderLoginLink_WithBlinkClass_WhenUserIsNotAuthenticated()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var loginAnchor = layout.Find($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        loginAnchor.GetAttribute("data-testid").ShouldBe(nameof(LoginLink.Elements.LoginLink));
        loginAnchor.ClassList.ShouldContain("login-link-blink");
        loginAnchor.GetAttribute("id").ShouldBe("login-link-blink");
        loginAnchor.TextContent.Trim().ShouldBe("Login");
        loginAnchor.GetAttribute("href").ShouldBe("/login");
    }

    [Test]
    public async Task ShouldExposeBlinkIdOnSameAnchorAsBlinkClassAndTestId_WhenUserIsNotAuthenticated()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var anchorsById = layout.FindAll("#login-link-blink");
        anchorsById.Count.ShouldBe(1);

        var loginAnchor = anchorsById[0];
        loginAnchor.TagName.ShouldBe("A");
        loginAnchor.ClassList.ShouldContain("login-link-blink");
        loginAnchor.GetAttribute("data-testid").ShouldBe(nameof(LoginLink.Elements.LoginLink));
        loginAnchor.GetAttribute("href").ShouldBe("/login");
    }

    [Test]
    public async Task ShouldNotExposeBlinkId_WhenUserIsAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        layout.FindAll("#login-link-blink").Count.ShouldBe(0);
        layout.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldPreserveLoginLinkHref_WhenBlinkClassApplied()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var loginAnchor = layout.Find($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        loginAnchor.ClassList.ShouldContain("login-link-blink");
        loginAnchor.GetAttribute("href").ShouldBe("/login");
    }

    [Test]
    public async Task ShouldNotRenderLoginLink_WhenUserIsAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        layout.FindAll($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']").Count.ShouldBe(0);
        layout.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldNotExposeBlinkClass_WhenUserIsAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        layout.FindAll(".login-link-blink").Count.ShouldBe(0);
        layout.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderCopyrightFooter_WithCurrentYear_OrganizationAndLink_WhenNotAuthenticated()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
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
    public async Task ShouldRenderCopyrightFooter_WhenAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        footer.TextContent.ShouldContain(yearText);
        footer.TextContent.ShouldContain("ClearMeasure Labs");
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link").GetAttribute("href")!.TrimEnd('/').ShouldBe("https://clearmeasure.com");
    }

    [Test]
    public async Task ShouldRenderSoftwareVersion_WithinSiteFooter()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var versionSpan = layout.Find($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']");
        versionSpan.TextContent.Trim().ShouldNotBeEmpty();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderSoftwareVersion_WhenUserIsAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var versionSpan = layout.Find($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']");
        versionSpan.ShouldNotBeNull();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderFooterNote_WithinSiteFooter()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var note = layout.Find($"[data-testid='{nameof(MainLayout.Elements.FooterNote)}']");
        note.TextContent.Trim().ShouldBe("Submit a new work order any time — requests are typically reviewed within one business day.");

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.FooterNote)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderCompanyLink_WithAccessibleAttributes_WhenExternalLinkUsesNewTab()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
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
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(true));

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());
        await toggle.ClickAsync(new());

        ctx.JSInterop.VerifyFocusAsyncInvoke();
    }

    [Test]
    public async Task ShouldRenderBackdrop_WhenNarrowAndNavOpen()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(true));

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        layout.FindAll(".nav-backdrop").Count.ShouldBe(1);
        layout.Find("#app-navigation-rail").ClassList.ShouldContain("open");
    }

    [Test]
    public async Task ShouldNotRenderBackdrop_WhenNarrowAndNavClosed()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(true));

        // Nav auto-hides on first narrow signal — backdrop should not be present
        layout.FindAll(".nav-backdrop").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldNotRenderBackdrop_OnWideViewport()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(false));

        layout.FindAll(".nav-backdrop").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldDismissNavWhenBackdropClicked()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
        });

        await component.InvokeAsync(() => layout.Instance.OnViewportChanged(true));

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        layout.FindAll(".nav-backdrop").Count.ShouldBe(1);

        var backdrop = layout.Find(".nav-backdrop");
        await backdrop.ClickAsync(new());

        layout.Find("#app-navigation-rail").ClassList.ShouldNotContain("open");
        layout.FindAll(".nav-backdrop").Count.ShouldBe(0);
    }


    [Test]
    public async Task DarkModeToggle_ShouldRender_WithSunIcon_WhenLightMode()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var button = layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']");
        button.ShouldNotBeNull();
        button.InnerHtml.ShouldContain("bi-sun");
    }

    [Test]
    public async Task DarkModeToggle_ShouldRender_WithMoonIcon_WhenDarkMode()
    {
        await using var ctx = CreateContext();
        var themeModule = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        themeModule.Setup<string>("getTheme").SetResult("dark");
        themeModule.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        themeModule.SetupVoid("setTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").InnerHtml.ShouldContain("bi-moon");
        });
    }

    [Test]
    public async Task DarkModeToggle_Click_ShouldFlipIsDarkMode()
    {
        await using var ctx = CreateContext();
        var themeModule = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        themeModule.Setup<string>("getTheme").SetResult("light");
        themeModule.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        themeModule.SetupVoid("setTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();
        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").ShouldNotBeNull();
        });

        var button = layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']");
        button.InnerHtml.ShouldContain("bi-sun");

        await button.ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").InnerHtml.ShouldContain("bi-moon");
        });
        ctx.Services.GetRequiredService<ThemePreferenceService>().IsDarkMode.ShouldBeTrue();
    }

    [Test]
    public async Task DarkModeToggle_ShouldBeVisible_WhenUserIsNotAuthenticated()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task DarkModeToggle_ShouldBeVisible_WhenUserIsAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").ShouldNotBeNull();
    }


    [Test]
    public async Task ShouldRenderGitSha_WithCorrectHref_WhenEndpointReturnsValidSha()
    {
        await using var ctx = CreateContext(gitSha: "abc1234def5678901");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var anchor = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        anchor.TagName.ShouldBe("A");
        var href = anchor.GetAttribute("href");
        href.ShouldNotBeNull();
        href.ShouldContain("github.com/ClearMeasureLabs/bootcamp-palermo-workorders/commit/abc1234def5678901");
    }

    [Test]
    public async Task ShouldTruncateGitSha_ToSevenChars_ForDisplayText()
    {
        await using var ctx = CreateContext(gitSha: "abc1234def5678901");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var anchor = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        anchor.TextContent.Trim().ShouldBe("abc1234");
    }

    [Test]
    public async Task ShouldRenderEnvironmentName_FromEndpointResponse()
    {
        await using var ctx = CreateContext(environmentName: "Staging");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("Staging");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenEndpointThrows()
    {
        await using var ctx = CreateContext(simulateHttpError: true);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForEnvironmentName_WhenEndpointThrows()
    {
        await using var ctx = CreateContext(simulateHttpError: true);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenResponseOmitsGitShaProperty()
    {
        await using var ctx = CreateContext(rawJsonBody: "{\"version\":\"1.0.0\"}");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForEnvironmentName_WhenResponseOmitsEnvironmentNameProperty()
    {
        await using var ctx = CreateContext(rawJsonBody: "{\"version\":\"1.0.0\"}");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenResponseBodyIsMalformedJson()
    {
        await using var ctx = CreateContext(rawJsonBody: "{not valid json");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderOpenWorkOrderCountBadge_WhenAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson", openWorkOrderCount: 7);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var badge = layout.Find($"[data-testid='{nameof(MainLayout.Elements.OpenWorkOrderCountBadge)}']");
        badge.TextContent.Trim().ShouldBe("7");
    }

    [Test]
    public async Task ShouldNotRenderOpenWorkOrderCountBadge_WhenUnauthenticated()
    {
        await using var ctx = CreateContext(openWorkOrderCount: 7);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        layout.FindAll($"[data-testid='{nameof(MainLayout.Elements.OpenWorkOrderCountBadge)}']").Count.ShouldBe(0);
    }

    private static BunitContext CreateContext(
        string? authenticateAsUser = null,
        string? gitSha = null,
        string? environmentName = null,
        bool simulateHttpError = false,
        string? rawJsonBody = null,
        int openWorkOrderCount = 0)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bunitAuth = ctx.AddAuthorization();
        if (authenticateAsUser != null)
        {
            bunitAuth.SetAuthorized(authenticateAsUser);
        }

        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus(openWorkOrderCount: openWorkOrderCount));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        if (authenticateAsUser != null)
        {
            customAuth.Login(authenticateAsUser).GetAwaiter().GetResult();
        }

        ctx.Services.AddSingleton(customAuth);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);

        var handler = new StubEnvironmentStatusHandler(
            simulateHttpError ? null : new EnvironmentStatusStub(
                Version: "1.0.0",
                GitSha: gitSha ?? "unknown",
                EnvironmentName: environmentName ?? "unknown"),
            rawJsonBody);
        ctx.Services.AddSingleton(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });

        return ctx;
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(null);
    }

    // ReSharper disable NotAccessedPositionalProperty.Local -- properties consumed via JSON serialization reflection
    private sealed record EnvironmentStatusStub(string Version, string GitSha, string EnvironmentName);
    // ReSharper restore NotAccessedPositionalProperty.Local

    private sealed class StubEnvironmentStatusHandler(EnvironmentStatusStub? stub, string? rawJsonBody = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (rawJsonBody is not null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(rawJsonBody, System.Text.Encoding.UTF8, "application/json")
                });
            }

            if (stub is null)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var json = JsonSerializer.Serialize(stub, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}

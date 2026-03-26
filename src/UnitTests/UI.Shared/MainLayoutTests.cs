using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
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

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.AddTestAuthorization();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        var auth = new CustomAuthenticationStateProvider();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(auth);
        return ctx;
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(null);
    }
}

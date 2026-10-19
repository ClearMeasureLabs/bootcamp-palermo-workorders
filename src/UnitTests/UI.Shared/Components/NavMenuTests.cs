using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class NavMenuTests
{
    [Test]
    public async Task ShouldRenderHomeOnly_WhenNoCurrentUser()
    {
        await using var ctx = CreateContext(currentUser: null);

        var component = ctx.Render<NavMenu>();

        component.Markup.ShouldContain("Home");
        component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Search)}']").Count.ShouldBe(0);
        component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Settings)}']").Count.ShouldBe(0);
        component.FindAll($"[data-testid='{nameof(NavMenu.Elements.AiAgent)}']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldRenderLoggedInNavItems_WhenCurrentUserPresent()
    {
        var user = CreateUserWithCreateRole();
        await using var ctx = CreateContext(user);

        var component = ctx.Render<NavMenu>();

        component.Find($"[data-testid='{nameof(NavMenu.Elements.Search)}']").ShouldNotBeNull();
        component.Find($"[data-testid='{nameof(NavMenu.Elements.Settings)}']").ShouldNotBeNull();
        component.Find($"[data-testid='{nameof(NavMenu.Elements.AiAgent)}']").ShouldNotBeNull();
        component.Find($"[data-testid='{nameof(NavMenu.Elements.NewWorkOrder)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldShowLoggedInState_WhenUserLoggedInEvent()
    {
        var user = CreateUserWithCreateRole();
        var session = new StubUserSession(null);
        await using var ctx = CreateContext(session);

        var component = ctx.Render<NavMenu>();
        component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Search)}']").Count.ShouldBe(0);

        session.CurrentUser = user;
        await component.InvokeAsync(() => component.Instance.Handle(new UserLoggedInEvent(user.UserName)));

        await component.WaitForAssertionAsync(() =>
            component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Search)}']").Count.ShouldBe(1));
    }

    [Test]
    public async Task ShouldClearLoggedInState_WhenUserLoggedOutEvent()
    {
        var user = CreateUserWithCreateRole();
        await using var ctx = CreateContext(user);

        var component = ctx.Render<NavMenu>();
        component.Find($"[data-testid='{nameof(NavMenu.Elements.Search)}']").ShouldNotBeNull();

        await component.InvokeAsync(() => component.Instance.Handle(new UserLoggedOutEvent(user.UserName)));

        component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Search)}']").Count.ShouldBe(0);
        component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Settings)}']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldToggleCollapseClass_WhenToggleButtonClicked()
    {
        await using var ctx = CreateContext(currentUser: null);

        var component = ctx.Render<NavMenu>();

        component.Find("div.nav-scrollable").ClassList.ShouldContain("collapse");

        await component.Find("button.navbar-toggler").ClickAsync();

        component.Find("div.nav-scrollable").ClassList.ShouldNotContain("collapse");

        await component.Find("button.navbar-toggler").ClickAsync();

        component.Find("div.nav-scrollable").ClassList.ShouldContain("collapse");
    }

    private static BunitContext CreateContext(Employee? currentUser)
    {
        return CreateContext(new StubUserSession(currentUser));
    }

    private static BunitContext CreateContext(StubUserSession session)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(session);
        return ctx;
    }

    private static Employee CreateUserWithCreateRole()
    {
        var user = new Employee("testuser", "Test", "User", "test@example.com");
        user.AddRole(new Role("creator", true, false));
        return user;
    }
}

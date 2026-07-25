using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class UserAvatarTests
{
    [Test]
    public void ShouldRenderAvatarWhenAuthenticated()
    {
        using var ctx = CreateContext(
            "jdoe",
            new Employee("jdoe", "Jane", "Doe", "jane@example.com"));

        var component = ctx.RenderComponent<UserAvatar>();

        var avatar = component.Find($"[data-testid='{nameof(UserAvatar.Elements.UserAvatar)}']");
        avatar.TextContent.Trim().ShouldBe("JD");
        avatar.ClassList.ShouldContain("user-avatar");
    }

    [Test]
    public void ShouldSetAriaLabel_ContainsDisplayName()
    {
        using var ctx = CreateContext(
            "jdoe",
            new Employee("jdoe", "Jane", "Doe", "jane@example.com"));

        var component = ctx.RenderComponent<UserAvatar>();

        var avatar = component.Find($"[data-testid='{nameof(UserAvatar.Elements.UserAvatar)}']");
        avatar.GetAttribute("aria-label").ShouldBe("Signed in as Jane Doe");
    }

    [Test]
    public void ShouldSetDataTestid_MatchesElements()
    {
        using var ctx = CreateContext(
            "jdoe",
            new Employee("jdoe", "Jane", "Doe", "jane@example.com"));

        var component = ctx.RenderComponent<UserAvatar>();

        component.FindAll($"[data-testid='{nameof(UserAvatar.Elements.UserAvatar)}']").Count.ShouldBe(1);
    }

    [Test]
    public void ShouldApplyBackgroundColorStyle_InlineOrCssVariable()
    {
        using var ctx = CreateContext(
            "jdoe",
            new Employee("jdoe", "Jane", "Doe", "jane@example.com"));

        var component = ctx.RenderComponent<UserAvatar>();

        var avatar = component.Find($"[data-testid='{nameof(UserAvatar.Elements.UserAvatar)}']");
        var style = avatar.GetAttribute("style");
        style.ShouldNotBeNull();
        style.ShouldContain("background-color:");
        style.ShouldContain(UserAvatarInitialsHelper.GetBackgroundColor("jdoe"));
    }

    [Test]
    public void ShouldNotRenderAvatarWhenUnauthenticated()
    {
        using var ctx = CreateContext(null, null);

        var component = ctx.RenderComponent<UserAvatar>();

        component.FindAll($"[data-testid='{nameof(UserAvatar.Elements.UserAvatar)}']").Count.ShouldBe(0);
    }

    [Test]
    public void ShouldHaveCorrectClassesForCircleStyle()
    {
        using var ctx = CreateContext(
            "jdoe",
            new Employee("jdoe", "Jane", "Doe", "jane@example.com"));

        var component = ctx.RenderComponent<UserAvatar>();

        var avatar = component.Find($"[data-testid='{nameof(UserAvatar.Elements.UserAvatar)}']");
        avatar.ClassList.ShouldContain("user-avatar");
    }

    private static TestContext CreateContext(string? username, Employee? employee)
    {
        var ctx = new TestContext();
        var authProvider = new CustomAuthenticationStateProvider();
        if (username != null)
            authProvider.Login(username);

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(employee));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        return ctx;
    }

    private sealed class StubUserSession(Employee? employee) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult(employee);
    }
}

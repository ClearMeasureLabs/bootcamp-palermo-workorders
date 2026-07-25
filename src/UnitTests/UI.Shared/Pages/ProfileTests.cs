using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class ProfileTests
{
    [Test]
    public void Should_RenderAccountSection_WithIdentityFields()
    {
        using var ctx = CreateContext();
        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Profile>());

        component.Find("h2").TextContent.ShouldContain("Account");
        component.Find($"[data-testid='{nameof(Profile.Elements.FullName)}']").TextContent.ShouldBe("Homer Simpson");
        component.Find($"[data-testid='{nameof(Profile.Elements.Username)}']").TextContent.ShouldBe("hsimpson");
        component.Find($"[data-testid='{nameof(Profile.Elements.Email)}']").TextContent.ShouldBe("homer@springfield.com");
    }

    [Test]
    public void Should_RenderLastLogin_AsFormattedDateTime_WhenLastLoginUtcIsSet()
    {
        using var ctx = CreateContext(new DateTimeOffset(2026, 7, 25, 15, 10, 0, TimeSpan.Zero));
        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Profile>());

        var lastLogin = component.Find($"[data-testid='{nameof(Profile.Elements.LastLogin)}']").TextContent;
        lastLogin.ShouldContain("Jul");
        lastLogin.ShouldContain("2026");
        lastLogin.ShouldContain("at");
        component.FindAll($"[data-testid='{nameof(Profile.Elements.FirstLoginHelper)}']").Count.ShouldBe(0);
    }

    [Test]
    public void Should_RenderFirstLoginHelper_WhenLastLoginUtcIsNull()
    {
        using var ctx = CreateContext(null);
        var component = ctx.RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Profile>());

        component.Find($"[data-testid='{nameof(Profile.Elements.LastLogin)}']").TextContent.ShouldBe("First login");
        component.Find($"[data-testid='{nameof(Profile.Elements.FirstLoginHelper)}']").TextContent
            .ShouldContain("No prior sign-in is recorded");
    }

    [Test]
    public void Should_RenderProfileLink_InHeader_WhenAuthenticated()
    {
        using var ctx = new TestContext();

        var authProvider = new CustomAuthenticationStateProvider();
        authProvider.Login("hsimpson");

        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new Bus(null!));

        var component = ctx.RenderComponent<ClearMeasure.Bootcamp.UI.Shared.Components.Logout>();

        var profileLink = component.Find($"[data-testid='{nameof(ClearMeasure.Bootcamp.UI.Shared.Components.Logout.Elements.ProfileLink)}']");
        profileLink.ShouldNotBeNull();
        profileLink.GetAttribute("href").ShouldBe("profile");
        profileLink.TextContent.ShouldBe("hsimpson");
    }

    private static TestContext CreateContext(DateTimeOffset? lastLoginUtc = null)
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new ProfileStubBus(lastLoginUtc));

        var bunitAuth = ctx.AddTestAuthorization();
        bunitAuth.SetAuthorized("hsimpson");
        var customAuth = new CustomAuthenticationStateProvider();
        customAuth.Login("hsimpson");
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);
        ctx.Services.AddSingleton(customAuth);

        return ctx;
    }

    private sealed class ProfileStubBus(DateTimeOffset? lastLoginUtc) : StubBus
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery)
            {
                var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com")
                {
                    LastLoginUtc = lastLoginUtc
                };
                return Task.FromResult<TResponse>((TResponse)(object)employee);
            }

            return base.Send(request);
        }
    }
}

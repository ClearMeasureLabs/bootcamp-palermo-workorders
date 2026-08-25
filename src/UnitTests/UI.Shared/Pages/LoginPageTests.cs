using Bunit;
using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class LoginPageTests
{
    [Test]
    public void ShouldOnlyRequireUsername()
    {
        var loginModel = new Login.LoginModel { Username = "hsimpson" };

        var validationContext = new ValidationContext(loginModel);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(loginModel, validationContext, validationResults, true);

        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRequireUsername()
    {
        var loginModel = new Login.LoginModel { Username = "" };

        var validationContext = new ValidationContext(loginModel);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(loginModel, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Username"));
    }

    [Test]
    public async Task ShouldDisplayDropdownWithEmployees()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var employeeSelect = component.Find($"[data-testid='{Login.Elements.User}']");
        employeeSelect.ShouldNotBeNull();
        employeeSelect.GetAttribute("id").ShouldBe(nameof(Login.Elements.User));
        component.Find($"label[for='{Login.Elements.User}']").ShouldNotBeNull();

        var options = component.FindAll("option");
        options.Count.ShouldBe(6);

        options[0].GetAttribute("value").ShouldBe(string.Empty);
        options[0].TextContent.ShouldBe("-- Select a parishioner or staff member --");
    }

    [Test]
    public async Task ShouldDisplayUppercaseLabelsInLoginDropdown_ForMixedAndAllCapsNames()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var hsimpsonOption = component.FindAll("option").Single(o => o.GetAttribute("value") == "hsimpson");
        hsimpsonOption.TextContent.ShouldBe("HOMER SIMPSON");

        var jdoeOption = component.FindAll("option").Single(o => o.GetAttribute("value") == "jdoe");
        jdoeOption.TextContent.ShouldBe("MARY JANE SIMPSON");
    }

    [Test]
    public async Task ShouldLoginWithSelectedEmployee()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var employeeSelect = component.Find($"[data-testid='{Login.Elements.User}']");
        var submitButton = component.Find($"[data-testid='{Login.Elements.LoginButton}']");

        await employeeSelect.ChangeAsync(new() { Value = "hsimpson" });
        await submitButton.ClickAsync(new());

        provider.IsAuthenticated().ShouldBeTrue();
        provider.GetUsername().ShouldBe("hsimpson");
    }

    [Test]
    public async Task ShouldDisplayFirstChurchOfShelbyvilleSubtitle()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var subtitle = component.Find(".login-subtitle");
        subtitle.TextContent.ShouldBe("First Church of Shelbyville");
    }

    [Test]
    public async Task Should_ShowLovejoyShortcut_WithoutDropdownSelection()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var shortcut = component.Find($"[data-testid='{Login.Elements.LovejoyShortcut}']");
        shortcut.ShouldNotBeNull();
        shortcut.TextContent.Trim().ShouldBe("Log in as Timothy Lovejoy");
        provider.IsAuthenticated().ShouldBeFalse();
    }

    [Test]
    public async Task Should_LoginAsTlovejoy_WhenLovejoyShortcutClicked()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var shortcut = component.Find($"[data-testid='{Login.Elements.LovejoyShortcut}']");
        await shortcut.ClickAsync(new());

        provider.IsAuthenticated().ShouldBeTrue();
        provider.GetUsername().ShouldBe("tlovejoy");
    }

    [Test]
    public async Task Should_LoginAsTlovejoy_WhenLovejoyClickedBeforeEmployeesLoaded()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var gatedBus = new GatedEmployeeStubBus();
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(gatedBus);

        var component = ctx.Render<Login>();

        component.FindAll("option")
            .Count(o => !string.IsNullOrEmpty(o.GetAttribute("value")))
            .ShouldBe(0);

        var shortcut = component.Find($"[data-testid='{Login.Elements.LovejoyShortcut}']");
        var clickTask = shortcut.ClickAsync(new());

        provider.IsAuthenticated().ShouldBeFalse();

        gatedBus.ReleaseEmployees();
        await clickTask;
        await component.WaitForAssertionAsync(() =>
        {
            provider.IsAuthenticated().ShouldBeTrue();
            provider.GetUsername().ShouldBe("tlovejoy");
        });
    }

    [Test]
    public async Task Should_KeepDropdownLoginUnchanged_WhenLovejoyShortcutPresent()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        component.Find($"[data-testid='{Login.Elements.LovejoyShortcut}']").ShouldNotBeNull();

        var employeeSelect = component.Find($"[data-testid='{Login.Elements.User}']");
        var submitButton = component.Find($"[data-testid='{Login.Elements.LoginButton}']");

        await employeeSelect.ChangeAsync(new() { Value = "hsimpson" });
        await submitButton.ClickAsync(new());

        provider.IsAuthenticated().ShouldBeTrue();
        provider.GetUsername().ShouldBe("hsimpson");
    }

    private sealed class GatedEmployeeStubBus : StubBus
    {
        private readonly TaskCompletionSource _gate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseEmployees() => _gate.TrySetResult();

        public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                await _gate.Task;
            }

            return await base.Send(request);
        }
    }
}
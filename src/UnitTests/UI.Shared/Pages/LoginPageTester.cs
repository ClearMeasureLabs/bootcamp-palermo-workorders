using System.ComponentModel.DataAnnotations;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class LoginPageTester
{
    [Test]
    public void ShouldOnlyRequireUsername()
    {
        var loginPage = new Login();
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
        var loginPage = new Login();
        var loginModel = new Login.LoginModel { Username = "" };

        var validationContext = new ValidationContext(loginModel);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(loginModel, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Username"));
    }

    [Test]
    public void ShouldDisplayDropdownWithEmployees()
    {
        using var ctx = new TestContext();

        var provider = new CustomAuthenticationStateProvider();
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.RenderComponent<Login>();

        var employeeSelect = component.Find($"[data-testid='{Login.Elements.User}']");
        employeeSelect.ShouldNotBeNull();

        var options = component.FindAll("option");
        options.Count.ShouldBe(4);
    }

    [Test]
    public void ShouldLoginWithSelectedEmployee()
    {
        using var ctx = new TestContext();

        var provider = new CustomAuthenticationStateProvider();
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.RenderComponent<Login>();

        var employeeSelect = component.Find($"[data-testid='{Login.Elements.User}']");
        var submitButton = component.Find($"[data-testid='{Login.Elements.LoginButton}']");

        employeeSelect.Change("hsimpson");
        submitButton.Click();

        provider.IsAuthenticated().ShouldBeTrue();
        provider.GetUsername().ShouldBe("hsimpson");
    }
}
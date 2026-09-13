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

        var mburnsOption = component.FindAll("option").Single(o => o.GetAttribute("value") == "mburns");
        mburnsOption.TextContent.ShouldBe("MONTGOMERY BURNS");
        mburnsOption.GetAttribute("value").ShouldBe("mburns");
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
    public async Task ShouldDisplayWelcomeToTheChurchPortalHeading()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var heading = component.Find("h3");
        heading.TextContent.ShouldBe("Welcome to the Church Portal");
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

    [Test]
    public async Task ShouldHaveAutofocusOnMemberDropdown()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var employeeSelect = component.Find($"[data-testid='{Login.Elements.User}']");
        employeeSelect.HasAttribute("autofocus").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldDisplaySignInHeading()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var heading = component.Find("h2");
        heading.TextContent.Trim().ShouldBe("Sign in");
    }

    [Test]
    public async Task ShouldDisplayLockIconInSignInHeading()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var icon = component.Find("h2 > i.bi-lock");
        icon.ShouldNotBeNull();
        icon.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Test]
    public async Task ShouldDisplayChooseYourNameSubtitle()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var subtitle = component.Find("p.text-muted");
        subtitle.TextContent.ShouldBe("Choose your name to continue");
    }

    [Test]
    public async Task Should_ShowHelperTextUnderMemberDropdown()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var helperText = component.Find("small.form-text.text-muted");
        helperText.ShouldNotBeNull();
        helperText.TextContent.ShouldBe("Not listed? Ask the church office to add you.");
    }

    [Test]
    public async Task Should_RenderForgotLoginLink_WithCorrectText()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("a[href='mailto:office@firstchurchofshelbyville.org']");
        link.TextContent.ShouldBe("Forgot your login?");
    }

    [Test]
    public async Task Should_RenderForgotLoginLink_WithCorrectHref()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("a[href='mailto:office@firstchurchofshelbyville.org']");
        link.GetAttribute("href").ShouldBe("mailto:office@firstchurchofshelbyville.org");
    }

    [Test]
    public async Task Should_RenderForgotLoginLink_InsideSmallTag()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("small > a[href='mailto:office@firstchurchofshelbyville.org']");
        link.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_RenderOfficePhoneLink_WithCorrectText()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("a[href='tel:+15550102468']");
        link.TextContent.ShouldBe("(555) 010-2468");
    }

    [Test]
    public async Task Should_RenderOfficePhoneLink_WithCorrectHref()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("a[href='tel:+15550102468']");
        link.GetAttribute("href").ShouldBe("tel:+15550102468");
    }

    [Test]
    public async Task Should_RenderOfficePhoneLink_InsideSmallTag()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("small > a[href='tel:+15550102468']");
        link.ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldDisplayCurrentYearFooter()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var footerDiv = component.FindAll("div.text-center")
            .First(d => d.QuerySelector("small.text-muted")?.TextContent
                .Contains("First Church of Shelbyville") == true);
        footerDiv.QuerySelector("small.text-muted")!.TextContent
            .ShouldBe("First Church of Shelbyville · " + DateTime.Now.Year);
    }

    [Test]
    public async Task EnterThePortalButton_ShouldHaveFullWidthClass()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var button = component.Find($"[data-testid='{Login.Elements.LoginButton}']");
        button.GetAttribute("class")!.ShouldContain("w-100");
    }

    [Test]
    public async Task EnterThePortalButton_ShouldPreserveBtnPrimaryClass()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var button = component.Find($"[data-testid='{Login.Elements.LoginButton}']");
        button.GetAttribute("class")!.ShouldContain("btn-primary");
    }

    [Test]
    public async Task EnterThePortalButton_ShouldPreserveSubmitType()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var button = component.Find($"[data-testid='{Login.Elements.LoginButton}']");
        button.GetAttribute("type").ShouldBe("submit");
    }

    [Test]
    public async Task EnterThePortalButton_ShouldPreserveButtonText()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var button = component.Find($"[data-testid='{Login.Elements.LoginButton}']");
        button.TextContent.Trim().ShouldBe("Enter the Portal");
    }

    [Test]
    public async Task Should_RenderRememberMySelectionCheckbox()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var checkbox = component.Find("input[type='checkbox'][id='rememberMySelection']");
        checkbox.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_RenderRememberMySelectionCheckbox_UncheckedByDefault()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var checkbox = component.Find("input[type='checkbox'][id='rememberMySelection']");
        checkbox.HasAttribute("checked").ShouldBeFalse();
    }

    [Test]
    public async Task Should_AssociateRememberMySelectionLabel_ViaForId()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var label = component.Find("label[for='rememberMySelection']");
        label.ShouldNotBeNull();
        label.TextContent.ShouldBe("Remember my selection");
    }

    [Test]
    public async Task Should_RenderNeedHelpLink_WithCorrectText()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("a[href='https://www.clear-measure.com']");
        link.TextContent.ShouldBe("Need help?");
    }

    [Test]
    public async Task Should_RenderNeedHelpLink_WithTargetBlank()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("a[href='https://www.clear-measure.com']");
        link.GetAttribute("target").ShouldBe("_blank");
    }

    [Test]
    public async Task Should_RenderNeedHelpLink_WithRelNoopener()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var link = component.Find("a[href='https://www.clear-measure.com']");
        link.GetAttribute("rel").ShouldBe("noopener noreferrer");
    }

    [Test]
    public async Task Should_ShowVersionLabel_WithVersionPrefix()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var versionLabel = component.FindAll("small.text-muted")
            .First(s => s.TextContent.StartsWith("Version "));
        versionLabel.TextContent.ShouldStartWith("Version ");
    }

    [Test]
    public async Task Should_ShowVersionLabel_WithNonEmptyVersionString()
    {
        await using var ctx = new BunitContext();

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(provider);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(provider);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Login>();

        var versionLabel = component.FindAll("small.text-muted")
            .First(s => s.TextContent.StartsWith("Version "));
        var versionString = versionLabel.TextContent["Version ".Length..];
        versionString.ShouldNotBeNullOrWhiteSpace();
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
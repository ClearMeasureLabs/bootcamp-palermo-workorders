using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LoginLinkTests
{
    [Test]
    public void ShouldRenderLoginAnchorWithHrefTestIdAndPromptClass()
    {
        using var ctx = new TestContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.RenderComponent<LoginLink>();

        var anchor = component.Find("a");
        anchor.GetAttribute("href").ShouldBe("/login");
        anchor.GetAttribute("data-testid").ShouldBe(nameof(LoginLink.Elements.LoginLink));
        anchor.GetAttribute("aria-label").ShouldBe("Sign in to the church portal");
        anchor.ClassList.ShouldContain("login-prompt-link");
        anchor.TextContent.ShouldBe("Login");
    }
}

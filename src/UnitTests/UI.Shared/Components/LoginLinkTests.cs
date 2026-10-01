using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LoginLinkTests
{
    [Test]
    public void ShouldRenderTooltipWithExactTitleOnLoginLink()
    {
        using var ctx = new BunitContext();
        var component = ctx.Render<LoginLink>();
        var link = component.Find($"[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        link.GetAttribute("title").ShouldBe("Sign in to manage work orders");
    }
}

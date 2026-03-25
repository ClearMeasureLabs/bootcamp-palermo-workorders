using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class HelloWorldTests
{
    [Test]
    public void ShouldRenderGreeting()
    {
        using var ctx = new TestContext();

        var component = ctx.RenderComponent<HelloWorld>();

        var greeting = component.Find($"[data-testid='{HelloWorld.Elements.Greeting}']");
        greeting.ShouldNotBeNull();
        greeting.TextContent.ShouldBe("Hello World");
    }

    [Test]
    public void ShouldHaveCorrectPageTitle()
    {
        using var ctx = new TestContext();

        var component = ctx.RenderComponent<HelloWorld>();

        var pageTitle = component.Find("h2");
        pageTitle.TextContent.ShouldBe("Hello World");
    }
}

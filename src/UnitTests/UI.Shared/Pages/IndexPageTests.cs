using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class IndexPageTests
{
    [Test]
    public void ShouldDisplayGreetingBanner()
    {
        using var ctx = new TestContext();

        var component = ctx.RenderComponent<Index>();

        var banner = component.Find($"[data-testid='{nameof(Index.Elements.GreetingBanner)}']");
        banner.ShouldNotBeNull();
        banner.TextContent.ShouldContain("Welcome to the AI Software Factory!");
    }
}

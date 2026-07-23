using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class CoinFlipControllerTests
{
    [Test]
    public void Get_Should_ReturnPlainTextHeadsOrTails()
    {
        var controller = new CoinFlipController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldBeOneOf("heads", "tails");
    }

    [Test]
    public void Get_Should_ReturnBothOutcomes_When_CalledRepeatedly()
    {
        var controller = new CoinFlipController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var outcomes = new HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            var content = controller.Get().ShouldBeOfType<ContentResult>();
            content.StatusCode.ShouldBe(200);
            content.Content.ShouldBeOneOf("heads", "tails");
            outcomes.Add(content.Content!);
        }

        outcomes.ShouldContain("heads");
        outcomes.ShouldContain("tails");
    }
}

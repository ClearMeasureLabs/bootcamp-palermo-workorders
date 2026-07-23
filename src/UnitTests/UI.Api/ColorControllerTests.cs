using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ColorControllerTests
{
    [Test]
    public void Get_Should_ReturnUppercaseHexColor()
    {
        var controller = new ColorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        content.Content!.Length.ShouldBe(7);
        content.Content[0].ShouldBe('#');
        foreach (var c in content.Content.AsSpan(1))
        {
            char.IsAsciiHexDigitUpper(c).ShouldBeTrue();
        }
    }

    [Test]
    public void Get_Should_ReturnDifferentColors_When_CalledTwice()
    {
        var controller = new ColorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var first = (controller.Get() as ContentResult)!.Content;
        var second = (controller.Get() as ContentResult)!.Content;

        first.ShouldNotBe(second);
    }
}

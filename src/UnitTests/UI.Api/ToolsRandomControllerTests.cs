using System.Globalization;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    [Test]
    public void Get_Should_ReturnPlainTextNumber_When_TypeIsNumber()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("number");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        int.TryParse(content.Content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n).ShouldBeTrue();
        n.ShouldBeGreaterThanOrEqualTo(int.MinValue);
        n.ShouldBeLessThanOrEqualTo(int.MaxValue);
    }

    [Test]
    public void Get_Should_ReturnPlainTextString_When_TypeIsString()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("string");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.Content.ShouldNotBeNull();
        content.Content!.Length.ShouldBe(ToolsRandomController.RandomStringLength);
        content.Content.ToCharArray().All(c => char.IsAsciiLetterOrDigit(c)).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnPlainTextUuid_When_TypeIsUuid()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("uuid");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.Content.ShouldNotBeNull();
        Guid.TryParse(content.Content, out var g).ShouldBeTrue();
        g.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Get_Should_ReturnUppercaseHexColor_When_TypeIsColor()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("color");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.Content.ShouldNotBeNull();
        content.Content!.Length.ShouldBe(7);
        content.Content[0].ShouldBe('#');
        content.Content[1..].All(c => char.IsAsciiHexDigitUpper(c)).ShouldBeTrue();
    }

    [TestCase("NUMBER")]
    [TestCase("Number")]
    public void Get_Should_AcceptTypeCaseInsensitively_When_Number(string type)
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(type);

        result.ShouldBeOfType<ContentResult>();
    }

    [Test]
    public void Get_Should_Return400_When_TypeIsNull()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_Return400_When_TypeIsEmpty()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("   ");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_Return400_When_TypeIsUnknown()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("not-a-real-type");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("Unknown");
    }

    [Test]
    public void Get_Should_ReturnDifferentNumbers_When_CalledTwice()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var a = (controller.Get("number") as ContentResult)!.Content!;
        var b = (controller.Get("number") as ContentResult)!.Content!;
        a.ShouldNotBe(b);
    }
}

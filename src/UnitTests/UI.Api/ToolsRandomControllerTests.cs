using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    [TestCase("number")]
    [TestCase("string")]
    [TestCase("uuid")]
    [TestCase("color")]
    [TestCase("NUMBER")]
    public void Get_Should_Return200WithMatchingTypeAndNonEmptyValue(string type)
    {
        var controller = CreateController();

        var result = controller.Get(type);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<ToolsRandomResponse>();
        payload.Type.ShouldBe(type.Trim().ToLowerInvariant());
        payload.Value.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_Should_ReturnParseableInteger_When_TypeIsNumber()
    {
        var controller = CreateController();

        var result = controller.Get("number");

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ToolsRandomResponse>();
        int.TryParse(payload.Value, out _).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnAlphanumeric_When_TypeIsString()
    {
        var controller = CreateController();

        var result = controller.Get("string");

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ToolsRandomResponse>();
        Regex.IsMatch(payload.Value, "^[A-Za-z0-9]+$").ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnGuid_When_TypeIsUuid()
    {
        var controller = CreateController();

        var result = controller.Get("uuid");

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ToolsRandomResponse>();
        Guid.TryParse(payload.Value, out _).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnHexColor_When_TypeIsColor()
    {
        var controller = CreateController();

        var result = controller.Get("color");

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ToolsRandomResponse>();
        Regex.IsMatch(payload.Value, "^#[0-9A-Fa-f]{6}$").ShouldBeTrue();
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_TypeMissing()
    {
        var controller = CreateController();

        var result = controller.Get(null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_TypeUnknown()
    {
        var controller = CreateController();

        var result = controller.Get("banana");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    private static ToolsRandomController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

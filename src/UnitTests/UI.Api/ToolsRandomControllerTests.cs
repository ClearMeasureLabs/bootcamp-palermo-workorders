using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    private static ToolsRandomController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Test]
    public void Should_ReturnPlainText200_When_GetNumberWithDefaults()
    {
        var controller = CreateController();

        var result = controller.Get("number", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        int.TryParse(content.Content, NumberStyles.Integer, CultureInfo.InvariantCulture, out _).ShouldBeTrue();
    }

    [Test]
    public void Should_ReturnPlainText200_When_GetNumberWithMinMax()
    {
        var controller = CreateController();

        var result = controller.Get("number", 50, 100, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        int.TryParse(content.Content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value).ShouldBeTrue();
        value.ShouldBeInRange(50, 100);
    }

    [Test]
    public void Should_ReturnPlainText200_When_GetStringWithDefaults()
    {
        var controller = CreateController();

        var result = controller.Get("string", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        content.Content!.Length.ShouldBe(ToolsRandomController.DefaultStringLength);
    }

    [Test]
    public void Should_ReturnPlainText200_When_GetStringWithCustomLength()
    {
        var controller = CreateController();

        var result = controller.Get("string", null, null, 20);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.Content!.Length.ShouldBe(20);
    }

    [Test]
    public void Should_ReturnPlainText200_When_GetUuid()
    {
        var controller = CreateController();

        var result = controller.Get("uuid", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        Guid.TryParse(content.Content, out _).ShouldBeTrue();
    }

    [Test]
    public void Should_ReturnPlainText200_When_GetColor()
    {
        var controller = CreateController();

        var result = controller.Get("color", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.Content.ShouldNotBeNull();
        Regex.IsMatch(content.Content!, "^#[0-9A-F]{6}$").ShouldBeTrue();
    }

    [Test]
    public void Should_ReturnPlainText200_When_GetVersionedEndpoint()
    {
        var routeAttributes = typeof(ToolsRandomController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(a => a.Template)
            .ToList();

        routeAttributes.ShouldContain("api/tools/random");
        routeAttributes.ShouldContain("api/v{version:apiVersion}/tools/random");
    }

    [Test]
    public void Should_ReturnBadRequest_When_TypeMissing()
    {
        var controller = CreateController();

        var result = controller.Get(null, null, null, null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("type");
    }

    [Test]
    public void Should_ReturnBadRequest_When_TypeInvalid()
    {
        var controller = CreateController();

        var result = controller.Get("invalid", null, null, null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("Invalid type");
    }

    [Test]
    public void Should_ReturnBadRequest_When_MinMaxInvalid()
    {
        var controller = CreateController();

        var result = controller.Get("number", 100, 50, null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("min");
    }

    [Test]
    public void Should_ReturnBadRequest_When_LengthTooLarge()
    {
        var controller = CreateController();

        var result = controller.Get("string", null, null, ToolsRandomController.MaxStringLength + 1);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("length");
    }
}

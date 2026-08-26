using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsGuidGeneratorControllerTests
{
    [Test]
    public void Post_Should_ReturnSingleGuidDFormat_When_CountOmitted()
    {
        var result = CreateController().Post();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var guids = ok.Value.ShouldBeAssignableTo<string[]>();
        guids.ShouldNotBeNull();
        guids.Length.ShouldBe(1);
        Guid.TryParseExact(guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnNGuids_When_CountExplicit()
    {
        var result = CreateController().Post(5);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var guids = ok.Value.ShouldBeAssignableTo<string[]>();
        guids.ShouldNotBeNull();
        guids.Length.ShouldBe(5);
        foreach (var guid in guids)
        {
            Guid.TryParseExact(guid, "D", out _).ShouldBeTrue();
        }

        guids.Distinct().Count().ShouldBe(5);
    }

    [Test]
    public void Post_Should_Return100Guids_When_CountAtMax()
    {
        var result = CreateController().Post(100);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var guids = ok.Value.ShouldBeAssignableTo<string[]>();
        guids.ShouldNotBeNull();
        guids.Length.ShouldBe(100);
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_CountBelowOne()
    {
        var result = CreateController().Post(0);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_CountAbove100()
    {
        var result = CreateController().Post(101);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    private static ToolsGuidGeneratorController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

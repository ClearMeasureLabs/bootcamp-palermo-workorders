using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    [Test]
    public void Post_Should_ReturnSingleGuid_When_CountOmitted()
    {
        var controller = CreateController();

        var result = controller.Post();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(1);
        Guid.TryParseExact(guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnNGuids_When_CountIsN()
    {
        const int n = 5;
        var controller = CreateController();

        var result = controller.Post(n);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(n);
        guids.Distinct().Count().ShouldBe(n);
        foreach (var g in guids)
            Guid.TryParseExact(g, "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_CountLessThanOne()
    {
        var controller = CreateController();

        var resultZero = controller.Post(0);
        var resultNegative = controller.Post(-3);

        AssertBadRequest(resultZero);
        AssertBadRequest(resultNegative);
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_CountGreaterThan100()
    {
        var controller = CreateController();

        var result = controller.Post(101);

        AssertBadRequest(result);
    }

    [Test]
    public void Post_Should_Return100Guids_When_CountIsMax()
    {
        var controller = CreateController();

        var result = controller.Post(100);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(100);
        guids.Distinct().Count().ShouldBe(100);
        foreach (var g in guids)
            Guid.TryParseExact(g, "D", out _).ShouldBeTrue();
    }

    private static GuidGeneratorController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static void AssertBadRequest(IActionResult result)
    {
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }
}

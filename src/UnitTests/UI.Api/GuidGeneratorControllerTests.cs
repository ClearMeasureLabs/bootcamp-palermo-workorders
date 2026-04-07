using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    [Test]
    public void Post_Should_ReturnOneCanonicalGuid_When_CountOmitted()
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(1);
        Guid.TryParseExact(payload.Guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnHundredDistinctGuids_When_CountIs100()
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post(100);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(100);
        payload.Guids.Select(Guid.Parse).ToHashSet().Count.ShouldBe(100);
    }

    [TestCase(0)]
    [TestCase(101)]
    [TestCase(-1)]
    public void Post_Should_Return400_When_CountOutOfRange(int count)
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post(count);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        problem.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Post_Should_Return400_When_CountIsNotInteger()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?count=not-a-number");
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        controller.ModelState.AddModelError("count", "The value 'not-a-number' is not valid for count.");

        var result = controller.Post();

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        problem.Value.ShouldBeOfType<ProblemDetails>();
    }
}

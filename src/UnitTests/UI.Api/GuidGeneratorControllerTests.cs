using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    private static GuidGeneratorController CreateController()
    {
        return new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Test]
    public void Post_Should_ReturnOneGuid_When_RequestIsNull()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(1);
        ShouldParseAsGuidD(payload.Guids[0]);
    }

    [Test]
    public void Post_Should_ReturnOneGuid_When_CountIsOmitted()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest());

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(1);
        ShouldParseAsGuidD(payload.Guids[0]);
    }

    [Test]
    public void Post_Should_ReturnDistinctGuids_When_CountIsThree()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(3));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(3);
        payload.Guids.Select(Guid.Parse).Distinct().Count().ShouldBe(3);
        foreach (var g in payload.Guids)
        {
            ShouldParseAsGuidD(g);
        }
    }

    [Test]
    public void Post_Should_ReturnProblem_When_CountIsZero()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(0));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Post_Should_ReturnProblem_When_CountIsNegative()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(-1));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Post_Should_ReturnProblem_When_CountExceedsMaximum()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(101));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Post_Should_ReturnHundredGuids_When_CountIsMaximum()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(100));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(100);
        payload.Guids.Select(Guid.Parse).Distinct().Count().ShouldBe(100);
    }

    private static void ShouldParseAsGuidD(string text)
    {
        Guid.TryParseExact(text, "D", out var parsed).ShouldBeTrue();
        parsed.ShouldNotBe(Guid.Empty);
    }
}

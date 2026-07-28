using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    [Test]
    public void Should_Post_ReturnSingleGuid_When_DefaultCount()
    {
        var controller = CreateController();

        var result = controller.Post(null, null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public void Should_Post_ReturnArrayOfGuids_When_CountSpecified()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(5), null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(5);
        payload.Guids.Distinct().Count().ShouldBe(5);
        foreach (var guid in payload.Guids)
        {
            Guid.TryParse(guid, out _).ShouldBeTrue();
        }
    }

    [Test]
    public void Should_Post_ReturnBadRequest_When_CountIsZero()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(0), null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_Post_ReturnBadRequest_When_CountExceedsMaximum()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(101), null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_Post_ReturnBadRequest_When_CountIsNegative()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(-1), null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_Post_ReturnValidGuidFormat_When_GuidsGenerated()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(3), null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        foreach (var guid in payload.Guids)
        {
            Guid.TryParse(guid, out var parsed).ShouldBeTrue();
            guid.ShouldBe(parsed.ToString("D"));
            guid.ShouldBe(guid.ToLowerInvariant());
        }
    }

    [Test]
    public void Should_Post_UseQueryStringCount_When_Provided()
    {
        var controller = CreateController();

        var result = controller.Post(null, 2);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(2);
    }

    private static GuidGeneratorController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

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
    public void Post_Should_ReturnSingleGuid_When_BodyNull()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(1);
        payload.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnSingleGuid_When_CountOmitted()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(null));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(1);
        payload.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnMultipleGuids_When_CountProvided()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(3));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(3);
        payload.Guids.Count.ShouldBe(3);
        foreach (var guid in payload.Guids)
        {
            Guid.TryParse(guid, out _).ShouldBeTrue();
        }
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_CountZero()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(0));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_CountAbove100()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(101));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnDistinctGuids_When_CountGreaterThanOne()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(10));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Distinct(StringComparer.Ordinal).Count().ShouldBe(10);
    }

    private static GuidGeneratorController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

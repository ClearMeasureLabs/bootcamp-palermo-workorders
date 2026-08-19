using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    [Test]
    public void Post_Should_ReturnOneGuid_When_BodyEmpty()
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
    public void Post_Should_ReturnRequestedCount_When_CountSpecified()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(5));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(5);
        payload.Guids.Count.ShouldBe(5);
        foreach (var guid in payload.Guids)
        {
            Guid.TryParse(guid, out _).ShouldBeTrue();
        }
    }

    [Test]
    public void Post_Should_ReturnMaxCount_When_CountIs100()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(100));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(100);
        payload.Guids.Count.ShouldBe(100);
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_CountBelowOne()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(0));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_CountAboveMax()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(101));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnUniqueGuids_When_MultipleRequested()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(10));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Distinct().Count().ShouldBe(10);
    }

    private static GuidGeneratorController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

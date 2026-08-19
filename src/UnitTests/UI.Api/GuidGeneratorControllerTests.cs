using ClearMeasure.Bootcamp.UI.Api;
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
    public void Post_DefaultCount_Should_ReturnSingleGuid()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(1);
        payload.Guids.Length.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public void Post_ExplicitCount_Should_ReturnMultipleGuids()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(5));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(5);
        payload.Guids.Length.ShouldBe(5);
        payload.Guids.Distinct().Count().ShouldBe(5);
    }

    [Test]
    public void Post_Count1_Should_ReturnSingleGuid()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(1));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(1);
        payload.Guids.Length.ShouldBe(1);
    }

    [Test]
    public void Post_Count100_Should_ReturnHundredGuids()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(100));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Count.ShouldBe(100);
        payload.Guids.Length.ShouldBe(100);
    }

    [Test]
    public void Post_CountZero_Should_Return400BadRequest()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(0));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_CountNegative_Should_Return400BadRequest()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(-1));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Count101_Should_Return400BadRequest()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(101));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnValidGuidFormat()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(3));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        foreach (var guid in payload.Guids)
        {
            Guid.TryParse(guid, out _).ShouldBeTrue();
        }
    }

    [Test]
    public void Post_Should_ReturnUniqueGuids()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(10));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Distinct().Count().ShouldBe(10);
    }

    [Test]
    public void Post_Status200_ContentTypeJson()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(1));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
    }
}

using System.Globalization;
using System.Net.Mime;
using System.Reflection;
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
    public void Post_Should_ReturnOkWith200AndGuidArray_WhenCountOmitted()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(1);
        Guid.TryParseExact(payload.Guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnOkWith200AndGuidArray_WhenCountExplicit()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(5));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(5);
        foreach (var guid in payload.Guids)
            Guid.TryParseExact(guid, "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnOkWith100Guids_WhenCountEqualsMax()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(100));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(100);
    }

    [Test]
    public void Post_Should_ReturnOkWith1Guid_WhenCountEqualsMin()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(1));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(1);
        Guid.TryParseExact(payload.Guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnBadRequest400_WhenCountBelowMin()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(0));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnBadRequest400_WhenCountAboveMax()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(101));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnBadRequest400_WhenCountIsNegative()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(-1));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnOkWithJsonContentType()
    {
        var method = typeof(GuidGeneratorController).GetMethod(nameof(GuidGeneratorController.Post));
        method.ShouldNotBeNull();
        var produces = method!.GetCustomAttributes<ProducesAttribute>(inherit: false)
            .SelectMany(a => a.ContentTypes);
        produces.ShouldContain(MediaTypeNames.Application.Json);
    }

    [Test]
    public void Post_Should_ReturnGuidsInStandardDFormat()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(3));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        foreach (var guid in payload.Guids)
        {
            Guid.TryParseExact(guid, "D", out var parsed).ShouldBeTrue();
            parsed.ToString("D", CultureInfo.InvariantCulture).ShouldBe(guid);
        }
    }
}

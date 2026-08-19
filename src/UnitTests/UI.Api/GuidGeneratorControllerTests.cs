using System.Globalization;
using System.Net.Mime;
using System.Text;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    private static GuidGeneratorController CreateController(Action<HttpRequest>? configureRequest = null)
    {
        var context = new DefaultHttpContext();
        configureRequest?.Invoke(context.Request);
        return new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    [Test]
    public async Task Post_Should_ReturnOkWith200AndGuidArray_WhenCountOmitted()
    {
        var controller = CreateController();

        var result = await controller.Post(CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(1);
        Guid.TryParseExact(payload.Guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Post_Should_ReturnOkWith200AndGuidArray_WhenCountExplicit()
    {
        var controller = CreateController(request =>
        {
            var json = "{\"count\":5}";
            var bytes = Encoding.UTF8.GetBytes(json);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
            request.ContentType = MediaTypeNames.Application.Json;
        });

        var result = await controller.Post(CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(5);
        foreach (var guid in payload.Guids)
        {
            Guid.TryParseExact(guid, "D", out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Post_Should_ReturnOkWith100Guids_WhenCountEqualsMax()
    {
        var controller = CreateController(request =>
        {
            var json = "{\"count\":100}";
            var bytes = Encoding.UTF8.GetBytes(json);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
            request.ContentType = MediaTypeNames.Application.Json;
        });

        var result = await controller.Post(CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(100);
    }

    [Test]
    public async Task Post_Should_ReturnOkWith1Guid_WhenCountEqualsMin()
    {
        var controller = CreateController(request =>
        {
            var json = "{\"count\":1}";
            var bytes = Encoding.UTF8.GetBytes(json);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
            request.ContentType = MediaTypeNames.Application.Json;
        });

        var result = await controller.Post(CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Length.ShouldBe(1);
        Guid.TryParseExact(payload.Guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Post_Should_ReturnBadRequest400_WhenCountBelowMin()
    {
        var controller = CreateController(request =>
        {
            var json = "{\"count\":0}";
            var bytes = Encoding.UTF8.GetBytes(json);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
            request.ContentType = MediaTypeNames.Application.Json;
        });

        var result = await controller.Post(CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task Post_Should_ReturnBadRequest400_WhenCountAboveMax()
    {
        var controller = CreateController(request =>
        {
            var json = "{\"count\":101}";
            var bytes = Encoding.UTF8.GetBytes(json);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
            request.ContentType = MediaTypeNames.Application.Json;
        });

        var result = await controller.Post(CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task Post_Should_ReturnBadRequest400_WhenCountIsNegative()
    {
        var controller = CreateController(request =>
        {
            var json = "{\"count\":-1}";
            var bytes = Encoding.UTF8.GetBytes(json);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
            request.ContentType = MediaTypeNames.Application.Json;
        });

        var result = await controller.Post(CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnOkWithJsonContentType()
    {
        var method = typeof(GuidGeneratorController).GetMethod(nameof(GuidGeneratorController.Post))!;
        var produces = method.GetCustomAttributes(typeof(ProducesAttribute), inherit: true)
            .Cast<ProducesAttribute>()
            .SelectMany(p => p.ContentTypes)
            .ToList();
        produces.ShouldContain(MediaTypeNames.Application.Json);
    }

    [Test]
    public async Task Post_Should_ReturnGuidsInStandardDFormat()
    {
        var controller = CreateController(request =>
        {
            var json = "{\"count\":3}";
            var bytes = Encoding.UTF8.GetBytes(json);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
            request.ContentType = MediaTypeNames.Application.Json;
        });

        var result = await controller.Post(CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        foreach (var guid in payload.Guids)
        {
            Guid.TryParseExact(guid, "D", out var parsed).ShouldBeTrue();
            parsed.ToString("D", CultureInfo.InvariantCulture).ShouldBe(guid);
        }
    }
}

using System.Text;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorToolsControllerTests
{
    private static readonly System.Text.RegularExpressions.Regex GuidPattern = new(
        "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    [Test]
    public async Task Should_Post_ReturnSingleGuid_WhenBodyOmitted()
    {
        var controller = CreateControllerWithBody(null);

        var result = await controller.PostAsync();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(1);
        GuidPattern.IsMatch(guids[0]).ShouldBeTrue();
        Guid.TryParse(guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Post_ReturnSingleGuid_WhenBodyEmptyObject()
    {
        var controller = CreateControllerWithBody("{}");

        var result = await controller.PostAsync();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(1);
        GuidPattern.IsMatch(guids[0]).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Post_ReturnRequestedCount_WhenCountProvided()
    {
        var controller = CreateControllerWithBody("""{"count":3}""");

        var result = await controller.PostAsync();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(3);
        foreach (var g in guids)
        {
            GuidPattern.IsMatch(g).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_Post_ClampCountToOne_WhenCountBelowRange()
    {
        var controller = CreateControllerWithBody("""{"count":0}""");

        var result = await controller.PostAsync();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(1);
    }

    [Test]
    public async Task Should_Post_ClampCountToOneHundred_WhenCountAboveRange()
    {
        var controller = CreateControllerWithBody("""{"count":999}""");

        var result = await controller.PostAsync();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(100);
    }

    [Test]
    public async Task Should_Post_ReturnBadRequest_WhenJsonMalformed()
    {
        var controller = CreateControllerWithBody("{not json");

        var result = await controller.PostAsync();

        result.ShouldBeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Should_Post_ReturnBadRequest_WhenJsonIsWrongType()
    {
        var controller = CreateControllerWithBody("[]");

        var result = await controller.PostAsync();

        result.ShouldBeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Should_Post_UseDefaultCount_WhenCountNull()
    {
        var controller = CreateControllerWithBody("""{"count":null}""");

        var result = await controller.PostAsync();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var guids = ok.Value.ShouldBeOfType<string[]>();
        guids.Length.ShouldBe(1);
    }

    private static GuidGeneratorToolsController CreateControllerWithBody(string? json)
    {
        var httpContext = new DefaultHttpContext();
        if (json is null)
        {
            httpContext.Request.Body = Stream.Null;
            httpContext.Request.ContentLength = 0;
        }
        else
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            httpContext.Request.Body = new MemoryStream(bytes);
            httpContext.Request.ContentLength = bytes.Length;
        }

        httpContext.Request.ContentType = "application/json";

        return new GuidGeneratorToolsController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}

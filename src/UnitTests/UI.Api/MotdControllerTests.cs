using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MotdControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithConfiguredMessage()
    {
        var stubOptions = Options.Create(new MotdOptions { Message = "Test message of the day" });
        var controller = new MotdController(stubOptions)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<MotdResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Test message of the day");
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var stubOptions = Options.Create(new MotdOptions { Message = "Stable MOTD" });
        var httpContext = new DefaultHttpContext();
        var controller = new MotdController(stubOptions)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var first = controller.Get();
        first.ShouldBeOfType<ContentResult>();
        var etag = httpContext.Response.Headers.ETag.ToString();
        etag.ShouldNotBeEmpty();

        httpContext.Request.Headers.IfNoneMatch = etag;
        var second = controller.Get();

        second.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }
}

using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonEchoResponse_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.QueryString = new QueryString("?debug=1");
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        context.Request.Headers["User-Agent"] = "unit-test";

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");

        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.Query["debug"].ShouldBe(["1"]);
        payload.Scheme.ShouldBe("http");
        payload.Host.ShouldBe("localhost");
        payload.Headers["User-Agent"].ShouldBe(["unit-test"]);
    }
}

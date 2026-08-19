using System.Net;
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
    public void Get_Should_ReturnJsonWithReflectedRequest_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.PathBase = "/app";
        context.Request.QueryString = new QueryString("?trace=1");
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost", 7174);
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.Request.Headers["User-Agent"] = "EchoUnitTest/1.0";
        context.Request.Headers["Accept"] = "application/json";

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
        payload.PathBase.ShouldBe("/app");
        payload.QueryString.ShouldBe("?trace=1");
        payload.Scheme.ShouldBe("https");
        payload.Host.ShouldBe("localhost:7174");
        payload.RemoteIpAddress.ShouldBe("203.0.113.10");
        payload.Headers["User-Agent"].ShouldBe("EchoUnitTest/1.0");
        payload.Headers["Accept"].ShouldBe("application/json");
    }

    [Test]
    public void Get_Should_RedactSensitiveHeaders_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.Headers["Authorization"] = "Bearer secret-token";
        context.Request.Headers["Cookie"] = "session=abc123";
        context.Request.Headers["X-Api-Key"] = "super-secret";
        context.Request.Headers["User-Agent"] = "EchoUnitTest/1.0";

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers.ContainsKey("Authorization").ShouldBeFalse();
        payload.Headers.ContainsKey("Cookie").ShouldBeFalse();
        payload.Headers.ContainsKey("X-Api-Key").ShouldBeFalse();
        payload.Headers["User-Agent"].ShouldBe("EchoUnitTest/1.0");
    }

    [Test]
    public void Get_Should_IncludeQueryParameters_When_CalledWithQueryString()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.QueryString = new QueryString("?key1=val1&key2=val2");

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Query["key1"].ShouldBe("val1");
        payload.Query["key2"].ShouldBe("val2");
        payload.QueryString.ShouldBe("?key1=val1&key2=val2");
    }
}
